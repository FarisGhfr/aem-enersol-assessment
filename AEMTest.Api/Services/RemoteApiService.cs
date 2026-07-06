using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AEMTest.Api.Models.Dtos;

namespace AEMTest.Api.Services;

/// <summary>
/// Communicates with the remote AEM REST API.
/// Uses System.Text.Json with JsonUnknownTypeHandling.JsonNode to silently ignore 
/// extra/new keys in responses, making deserialization resilient to API changes.
/// </summary>
public class RemoteApiService : IRemoteApiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RemoteApiService> _logger;

    // Shared deserializer options: unknown properties are ignored automatically by default
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        // JsonSerializer ignores unknown JSON properties by default in System.Text.Json
        // This means if the API adds new keys, they are silently skipped — no exception thrown.
    };

    public RemoteApiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<RemoteApiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<string?> LoginAsync()
    {
        var apiConfig = _configuration.GetSection("RemoteApi");
        var baseUrl = apiConfig["BaseUrl"] ?? throw new InvalidOperationException("RemoteApi:BaseUrl is not configured.");
        var username = apiConfig["Username"] ?? throw new InvalidOperationException("RemoteApi:Username is not configured.");
        var password = apiConfig["Password"] ?? throw new InvalidOperationException("RemoteApi:Password is not configured.");

        var loginRequest = new LoginRequest
        {
            Username = username,
            Password = password
        };

        var json = JsonSerializer.Serialize(loginRequest, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogInformation("Attempting login to remote API at {BaseUrl}", baseUrl);

        try
        {
            var response = await _httpClient.PostAsync($"{baseUrl}/api/Account/Login", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Login failed with status {Status}: {Body}", response.StatusCode, errorBody);
                return null;
            }

            var responseBody = await response.Content.ReadAsStringAsync();

            // The remote API returns the token as a plain JWT string (not a JSON object).
            // Strip surrounding quotes if present (e.g., "\"eyJ...\"" → "eyJ...")
            var token = responseBody.Trim().Trim('"');

            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogError("Login succeeded but token was empty. Response: {Body}", responseBody);
                return null;
            }

            _logger.LogInformation("Login successful. Token acquired.");
            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during login.");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<List<PlatformDto>> GetPlatformWellActualAsync(string bearerToken)
    {
        var baseUrl = _configuration["RemoteApi:BaseUrl"]
            ?? throw new InvalidOperationException("RemoteApi:BaseUrl is not configured.");

        _logger.LogInformation("Fetching GetPlatformWellActual data...");

        try
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", bearerToken);

            var response = await _httpClient.GetAsync($"{baseUrl}/api/PlatformWell/GetPlatformWellActual");
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("GetPlatformWellActual raw response: {Body}", responseBody);

            // Deserialize: missing keys → null values. Extra new keys → silently ignored.
            var platforms = JsonSerializer.Deserialize<List<PlatformDto>>(responseBody, _jsonOptions)
                            ?? new List<PlatformDto>();

            _logger.LogInformation("Fetched {Count} platforms from GetPlatformWellActual.", platforms.Count);
            return platforms;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching GetPlatformWellActual.");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<PlatformDummyDto>> GetPlatformWellDummyAsync(string bearerToken)
    {
        var baseUrl = _configuration["RemoteApi:BaseUrl"]
            ?? throw new InvalidOperationException("RemoteApi:BaseUrl is not configured.");

        _logger.LogInformation("Fetching GetPlatformWellDummy data...");

        try
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", bearerToken);

            var response = await _httpClient.GetAsync($"{baseUrl}/api/PlatformWell/GetPlatformWellDummy");
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("GetPlatformWellDummy raw response: {Body}", responseBody);

            // Resilient deserialization: handles different key sets, missing fields, extra fields
            var platforms = JsonSerializer.Deserialize<List<PlatformDummyDto>>(responseBody, _jsonOptions)
                            ?? new List<PlatformDummyDto>();

            _logger.LogInformation("Fetched {Count} platforms from GetPlatformWellDummy.", platforms.Count);
            return platforms;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching GetPlatformWellDummy.");
            throw;
        }
    }
}
