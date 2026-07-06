using System.Text.Json.Serialization;

namespace AEMTest.Api.Models.Dtos;

/// <summary>
/// Represents a Well from the GetPlatformWellDummy API response.
/// Dummy API uses "lastUpdate" instead of "createdAt"/"updatedAt".
/// Unknown new keys are automatically ignored by System.Text.Json.
/// </summary>
public class WellDummyDto
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

    /// <summary>
    /// Dummy API uses "lastUpdate" instead of "createdAt"/"updatedAt".
    /// </summary>
    [JsonPropertyName("lastUpdate")]
    public DateTime? LastUpdate { get; set; }

    /// <summary>
    /// Kept for forward compatibility with actual API fields (will be null in dummy response).
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}
