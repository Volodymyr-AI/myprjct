using System.Text.Json;
using PMSIntegration.Application.Logging;
using PMSIntegration.Core.Configuration.Models;
using PMSIntegration.Core.Patients.Models;
using PMSIntegration.Infrastructure.OpenDental.DTO;

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
            BaseAddress = new Uri(_config.ApiBaseUrl),
            Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
        };
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue(_config.AuthScheme, _config.AuthToken);

        _jsonOptions = new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        };
    }
    
    /// <summary>
    /// Check if OpenDental API is available and responding
    /// </summary>
    public async Task<bool> IsApiAvailable()
    {
        try
        {
            _logger.LogDebug("Checking OpenDental API availability...");
            
            // Simple health check - try to get 1 patient
            var response = await _httpClient.GetAsync("/api/v1/patients/Simple?limit=1");
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInfo("OpenDental API is available and responding");
                return true;
            }
            
            _logger.LogWarn($"OpenDental API returned status: {response.StatusCode}");
            
            // Check if it's an auth issue
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogError(new UnauthorizedAccessException(), 
                    "OpenDental API authentication failed. Check AuthToken in configuration.");
            }
            
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Cannot connect to OpenDental API. Is OpenDental running?");
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, $"OpenDental API request timed out after {_config.TimeoutSeconds} seconds");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error checking OpenDental API availability");
            return false;
        }
    }
    
    /// <summary>
    /// Export patients from OpenDental
    /// </summary>
    public async Task<List<Patient>> ExportPatientsAsync(DateTime? fromDate = null)
    {
        try
        {
            var dateFilter = fromDate ?? _config.ExportStartDate.DateTime;
            var dateStamp = dateFilter.ToString("yyyy-MM-dd HH:mm:ss");
            
            _logger.LogInfo($"Exporting patients from OpenDental API with DateTStamp >= {dateStamp}");
            
            // Use Simple endpoint with DateTStamp filter
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
    
    /// <summary>
    /// Parse patients from JSON response
    /// </summary>
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
    
    /// <summary>
    /// Export single patient by ID
    /// </summary>
    public async Task<Patient?> ExportPatientByIdAsync(int patientId)
    {
        try
        {
            _logger.LogDebug($"Fetching patient by ID: {patientId}");
            
            var response = await _httpClient.GetAsync($"/api/v1/patients?PatNum={patientId}");
            
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogDebug($"Patient {patientId} not found");
                }
                else
                {
                    _logger.LogWarn($"Failed to fetch patient {patientId}: {response.StatusCode}");
                }
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
    
    /// <summary>
    /// Export insurance data for a specific patient
    /// Returns DTOs that can be mapped to Insurance or OpenDentalInsurance
    /// </summary>
    public async Task<List<OpenDentalInsuranceDto>> ExportPatientInsuranceAsync(int patientId)
    {
        try
        {
            _logger.LogDebug($"Exporting insurance data for patient: {patientId}");
        
            var requestUrl = $"/api/v1/familymodules/{patientId}/Insurance";
            var response = await _httpClient.GetAsync(requestUrl);
        
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogDebug($"No insurance found for patient {patientId}");
                    return new List<OpenDentalInsuranceDto>();
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError(new Exception($"API call failed: {response.StatusCode}"), 
                    $"OpenDental Insurance API error: {errorContent}");
                return new List<OpenDentalInsuranceDto>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var insuranceList = await ParseInsuranceDtosFromJsonAsync(content);

            if (insuranceList.Any())
            {
                _logger.LogDebug($"Successfully exported {insuranceList.Count} insurance plans for patient {patientId}");
            }
            
            return insuranceList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Exception occurred while exporting insurance for patient {patientId}");
            return new List<OpenDentalInsuranceDto>();
        }
    }
    
    /// <summary>
    /// Parse insurance DTOs from JSON response
    /// </summary>
    private async Task<List<OpenDentalInsuranceDto>> ParseInsuranceDtosFromJsonAsync(string jsonContent)
    {
        try
        {
            var dtos = JsonSerializer.Deserialize<List<OpenDentalInsuranceDto>>(jsonContent, _jsonOptions);
            
            if (dtos == null)
            {
                _logger.LogWarn("Failed to deserialize insurance data from OpenDental API");
                return new List<OpenDentalInsuranceDto>();
            }
            
            _logger.LogDebug($"Parsed {dtos.Count} insurance DTOs from JSON");
            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing insurance DTOs from JSON");
            return new List<OpenDentalInsuranceDto>();
        }
    }
    
    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}