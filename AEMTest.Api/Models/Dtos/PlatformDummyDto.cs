using System.Text.Json.Serialization;

namespace AEMTest.Api.Models.Dtos;

/// <summary>
/// Represents a Platform from the GetPlatformWellDummy API response.
/// This endpoint returns DIFFERENT keys — it uses "lastUpdate" instead of "createdAt"/"updatedAt".
/// This simulates real-world API changes our app must handle without breaking.
/// All fields are nullable; only known fields are mapped. Unknown keys are silently ignored.
/// </summary>
public class PlatformDummyDto
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("uniqueName")]
    public string? UniqueName { get; set; }

    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }

    /// <summary>
    /// Dummy API uses "lastUpdate" instead of "createdAt"/"updatedAt".
    /// We map both so we can handle either variant gracefully.
    /// </summary>
    [JsonPropertyName("lastUpdate")]
    public DateTime? LastUpdate { get; set; }

    /// <summary>
    /// These will be null for the Dummy API — kept for forward compatibility.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Nested well list — the API uses "well" (singular) as the JSON key.
    /// </summary>
    [JsonPropertyName("well")]
    public List<WellDummyDto>? Wells { get; set; }
}
