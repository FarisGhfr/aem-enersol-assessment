using System.Text.Json.Serialization;

namespace AEMTest.Api.Models.Dtos;

/// <summary>
/// Represents a Well nested under a Platform in the GetPlatformWellActual API response.
/// All fields are nullable to handle missing keys gracefully (resilient deserialization).
/// Unknown new keys in the JSON are automatically ignored by System.Text.Json.
/// </summary>
public class WellDto
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("platformId")]
    public int? PlatformId { get; set; }

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
}
