using System.Text.Json.Serialization;

namespace AEMTest.Api.Models.Dtos;

/// <summary>
/// Request body for the remote API Login endpoint.
/// </summary>
public class LoginRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}
