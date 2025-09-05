using System.Text.Json;
using PMSIntegration.Application.Logging;
using PMSIntegration.Core.Configuration.Models;
using PMSIntegration.Core.Patients.Models;
using PMSIntegration.Core.Patients.ValueObjects;
using PMSIntegration.Infrastructure.OpenDental.DTO;
using PMSIntegration.Infrastructure.OpenDental.Extensions;

namespace PMSIntegration.Infrastructure.OpenDental.Services;

public class OpenDentalService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly OpenDentalIntegrationConfig _config;
    private readonly FileLogger _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public OpenDentalService(OpenDentalIntegrationConfig config, FileLogger logger)
    {
        _config = config;
        _logger = logger;
        
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_config.ApiBaseUrl), // Using URL from config
            Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
        };
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue(_config.AuthScheme, _config.AuthToken);

        _jsonOptions = new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        };
    }
    
    public async Task<List<Patient>> ExportPatientsAsync(DateTime? fromDate = null)
    {
        try
        {
            var dateFilter = fromDate ?? _config.ExportStartDate.DateTime;
            var dateStamp = dateFilter.ToString("yyyy-MM-dd HH:mm:ss");
            
            _logger.LogInfo($"Exporting patients from OpenDental API with DateTStamp >= {dateStamp}");
            
            // Use Simple endpoint with DateTStamp filter
            //var requestUrl = $"/api/v1/patients";
            var requestUrl = $"/api/v1/patients/Simple?DateTStamp={Uri.EscapeDataString(dateStamp)}";
            var response = await _httpClient.GetAsync(requestUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError(new Exception($"API call failed: {response.StatusCode}"), 
                    $"OpenDental API error: {errorContent}");
                return new List<Patient>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var patients = await ParsePatientsFromJsonAsync(content);

            _logger.LogInfo($"Successfully exported {patients.Count} patients from OpenDental");
            return patients;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while exporting patients from OpenDental");
            return new List<Patient>();
        }
    }
    
    private async Task<List<Patient>> ParsePatientsFromJsonAsync(string jsonContent)
    {
        try
        {
            var rawPatients = JsonSerializer.Deserialize<List<OpenDentalPatientDto>>(jsonContent, _jsonOptions);

            if (rawPatients == null)
            {
                _logger.LogWarn("Failed to deserialize patient data from OpenDental API");
                return new List<Patient>();
            }

            var patients = new List<Patient>();
            
            foreach (var rawPatient in rawPatients)
            {
                try
                {
                    var patient = rawPatient.ToDomain();
                    patients.Add(patient);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error converting patient {rawPatient.PatNum} to domain model");
                }
            }

            return patients;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing patients from JSON response");
            return new List<Patient>();
        }
    }
    
    public async Task<Patient?> ExportPatientByIdAsync(int patientId)
    {
        try
        {
            _logger.LogInfo($"Fetching patient by ID: {patientId}");
            
            var response = await _httpClient.GetAsync($"/api/v1/patients?PatNum={patientId}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarn($"Patient {patientId} not found: {response.StatusCode}");
                return null;
            }
            
            var content = await response.Content.ReadAsStringAsync();
            var patients = await ParsePatientsFromJsonAsync(content);
            return patients.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Exception while fetching patient by ID {patientId}");
            return null;
        }
    }
    
    // ======== INSURANCE ========
    public async Task<List<PatientInsurance>> ExportPatientInsuranceAsync(int patientId)
    {
        try
        {
            _logger.LogInfo($"Exporting insurance data for patient: {patientId}");
        
            var requestUrl = $"/api/v1/familymodules/{patientId}/Insurance";
            var response = await _httpClient.GetAsync(requestUrl);
        
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError(new Exception($"API call failed: {response.StatusCode}"), 
                    $"OpenDental Insurance API error: {errorContent}");
                return new List<PatientInsurance>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var insuranceList = await ParseInsuranceFromJsonAsync(content);

            _logger.LogInfo($"Successfully exported {insuranceList.Count} insurance plans for patient {patientId}");
            return insuranceList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Exception occurred while exporting insurance for patient {patientId}");
            return new List<PatientInsurance>();
        }
    }
    
    public async Task<List<PatientInsurance>> ExportAllPatientsInsuranceAsync(List<int> patientIds)
    {
        var allInsurance = new List<PatientInsurance>();
    
        foreach (var patientId in patientIds)
        {
            var patientInsurance = await ExportPatientInsuranceAsync(patientId);
            allInsurance.AddRange(patientInsurance);
        
            // Small delay to avoid overwhelming the API
            await Task.Delay(100);
        }
    
        return allInsurance;
    }
    
    private async Task<List<PatientInsurance>> ParseInsuranceFromJsonAsync(string jsonContent)
    {
        try
        {
            using var document = JsonDocument.Parse(jsonContent);
            var insuranceList = new List<PatientInsurance>();
        
            if (!document.RootElement.ValueKind.Equals(JsonValueKind.Array))
            {
                _logger.LogWarn("Expected JSON array but got different type");
                return new List<PatientInsurance>();
            }

            foreach (var element in document.RootElement.EnumerateArray())
            {
                try
                {
                    var insurance = ParseSingleInsuranceElement(element);
                    if (insurance != null)
                    {
                        insuranceList.Add(insurance);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing single insurance element");
                }
            }

            return insuranceList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing insurance from JSON response");
            return new List<PatientInsurance>();
        }
    }

    private PatientInsurance? ParseSingleInsuranceElement(JsonElement element)
    {
        try
        {
            var patNum = element.GetProperty("PatNum").GetInt32();
            var insSubNum = element.GetProperty("InsSubNum").GetInt32();
            var patPlanNum = element.GetProperty("PatPlanNum").GetInt32();
        
            var carrierName = GetStringProperty(element, "CarrierName");
            var subscriberId = GetStringProperty(element, "SubscriberID");
            var subscriberName = GetStringProperty(element, "subscriber");
            var relationship = GetStringProperty(element, "Relationship");
            var groupName = GetStringProperty(element, "GroupName");
            var groupNum = GetStringProperty(element, "GroupNum");
            var planTypeDesc = GetStringProperty(element, "planType");
            var ordinalDesc = GetStringProperty(element, "ordinal");
            var employerName = GetStringProperty(element, "employer");
            var planNote = GetStringProperty(element, "PlanNote");
        
            var isPending = GetStringProperty(element, "IsPending")?.ToLower() == "true";
            var isMedical = GetStringProperty(element, "IsMedical")?.ToLower() == "true";

            return PatientInsurance.Create(
                patientId: new PatientId(patNum),
                insSubNum: insSubNum,
                patPlanNum: patPlanNum,
                carrierName: carrierName,
                subscriberId: subscriberId,
                subscriberName: subscriberName,
                relationship: relationship,
                groupName: groupName,
                groupNumber: groupNum,
                planType: planTypeDesc,
                ordinal: ordinalDesc,
                isPending: isPending,
                isMedical: isMedical,
                employerName: employerName,
                planNote: planNote
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PatientInsurance from JSON element");
            return null;
        }
    }
    
    private static string GetStringProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && 
               property.ValueKind == JsonValueKind.String 
            ? property.GetString() ?? ""
            : "";
    }
    
    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}