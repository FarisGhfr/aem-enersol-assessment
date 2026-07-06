using System.Text.Json.Serialization;

namespace AEMTest.Api.Models.Dtos;

/// <summary>
/// Represents a Platform from the GetPlatformWellActual API response.
/// All fields are nullable to handle missing keys gracefully (resilient deserialization).
/// Unknown new keys in the JSON are automatically ignored by System.Text.Json.
/// </summary>
public class PlatformDto
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("uniqueName")]
    public string? UniqueName { get; set; }

    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Nested well list — the API uses "well" (singular) as the JSON key.
    /// May be null if key is missing.
    /// </summary>
    [JsonPropertyName("well")]
    public List<WellDto>? Wells { get; set; }
}
