using System.Text.Json;
using PMSIntegration.Application.Logging;
using PMSIntegration.Core.Configuration.Models;
using PMSIntegration.Core.Patients.Models;
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
            BaseAddress = new Uri("http://localhost:30222"), // TODO: OpenDental default uri move to config or straight to db
            Timeout = TimeSpan.FromSeconds(30)
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
            var requestUrl = $"/api/v1/patients";
            //var requestUrl = $"/api/v1/patients/Simple?DateTStamp={Uri.EscapeDataString(dateStamp)}";
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

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}