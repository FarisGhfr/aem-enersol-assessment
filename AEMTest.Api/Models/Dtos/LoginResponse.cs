using System.Text.Json.Serialization;

namespace AEMTest.Api.Models.Dtos;

/// <summary>
/// Response from the remote API Login endpoint.
/// All fields are nullable so the app does not break if structure changes.
/// </summary>
public class LoginResponse
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("expiration")]
    public DateTime? Expiration { get; set; }
}
