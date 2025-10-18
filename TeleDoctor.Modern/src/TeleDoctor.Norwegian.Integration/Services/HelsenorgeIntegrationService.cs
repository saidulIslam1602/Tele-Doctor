using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;
using TeleDoctor.Norwegian.Integration.Models;

namespace TeleDoctor.Norwegian.Integration.Services;

public interface IHelsenorgeIntegrationService
{
    Task<HelsenorgePatientData?> GetPatientDataAsync(string personalNumber);
    Task<List<HelsenorgeMedication>> GetPatientMedicationsAsync(string personalNumber);
    Task<bool> SendConsultationSummaryAsync(string personalNumber, HelsenorgeConsultationSummary summary);
    Task<List<HelsenorgeAppointment>> GetPatientAppointmentsAsync(string personalNumber);
    Task<bool> CreateAppointmentAsync(HelsenorgeAppointment appointment);
}

public class HelsenorgeIntegrationService : IHelsenorgeIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HelsenorgeIntegrationService> _logger;
    private readonly string _baseUrl;
    private readonly string _apiKey;

    public HelsenorgeIntegrationService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<HelsenorgeIntegrationService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _baseUrl = configuration["Norwegian:HelsenorgeIntegration:BaseUrl"] ?? throw new ArgumentException("Helsenorge BaseUrl not configured");
        _apiKey = configuration["Norwegian:HelsenorgeIntegration:ApiKey"] ?? throw new ArgumentException("Helsenorge ApiKey not configured");
        
        ConfigureHttpClient();
    }

    public async Task<HelsenorgePatientData?> GetPatientDataAsync(string personalNumber)
    {
        try
        {
            _logger.LogInformation("Fetching patient data from Helsenorge for personal number: {PersonalNumber}", 
                personalNumber.Substring(0, 6) + "****"); // Log only first 6 digits for privacy

            var response = await _httpClient.GetAsync($"api/v1/patients/{personalNumber}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var patientData = JsonSerializer.Deserialize<HelsenorgePatientData>(content, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                
                _logger.LogInformation("Successfully retrieved patient data from Helsenorge");
                return patientData;
            }
            else
            {
                _logger.LogWarning("Failed to retrieve patient data from Helsenorge. Status: {StatusCode}", response.StatusCode);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving patient data from Helsenorge");
            return null;
        }
    }

    public async Task<List<HelsenorgeMedication>> GetPatientMedicationsAsync(string personalNumber)
    {
        try
        {
            _logger.LogInformation("Fetching patient medications from Helsenorge");

            var response = await _httpClient.GetAsync($"api/v1/patients/{personalNumber}/medications");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var medications = JsonSerializer.Deserialize<List<HelsenorgeMedication>>(content, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }) ?? new List<HelsenorgeMedication>();
                
                _logger.LogInformation("Successfully retrieved {Count} medications from Helsenorge", medications.Count);
                return medications;
            }
            else
            {
                _logger.LogWarning("Failed to retrieve medications from Helsenorge. Status: {StatusCode}", response.StatusCode);
                return new List<HelsenorgeMedication>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving medications from Helsenorge");
            return new List<HelsenorgeMedication>();
        }
    }

    public async Task<bool> SendConsultationSummaryAsync(string personalNumber, HelsenorgeConsultationSummary summary)
    {
        try
        {
            _logger.LogInformation("Sending consultation summary to Helsenorge");

            var json = JsonSerializer.Serialize(summary, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"api/v1/patients/{personalNumber}/consultations", content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully sent consultation summary to Helsenorge");
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to send consultation summary to Helsenorge. Status: {StatusCode}", response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending consultation summary to Helsenorge");
            return false;
        }
    }

    public async Task<List<HelsenorgeAppointment>> GetPatientAppointmentsAsync(string personalNumber)
    {
        try
        {
            _logger.LogInformation("Fetching patient appointments from Helsenorge");

            var response = await _httpClient.GetAsync($"api/v1/patients/{personalNumber}/appointments");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var appointments = JsonSerializer.Deserialize<List<HelsenorgeAppointment>>(content, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }) ?? new List<HelsenorgeAppointment>();
                
                _logger.LogInformation("Successfully retrieved {Count} appointments from Helsenorge", appointments.Count);
                return appointments;
            }
            else
            {
                _logger.LogWarning("Failed to retrieve appointments from Helsenorge. Status: {StatusCode}", response.StatusCode);
                return new List<HelsenorgeAppointment>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appointments from Helsenorge");
            return new List<HelsenorgeAppointment>();
        }
    }

    public async Task<bool> CreateAppointmentAsync(HelsenorgeAppointment appointment)
    {
        try
        {
            _logger.LogInformation("Creating appointment in Helsenorge");

            var json = JsonSerializer.Serialize(appointment, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/v1/appointments", content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully created appointment in Helsenorge");
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to create appointment in Helsenorge. Status: {StatusCode}", response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating appointment in Helsenorge");
            return false;
        }
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "TeleDoctor-Modern/1.0");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "nb-NO,nb;q=0.9,no;q=0.8,nn;q=0.7,en;q=0.6");
    }
}
