using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AEMTest.Api.Models.Entities;

/// <summary>
/// Represents a Platform record stored in the database.
/// Maps fields from the remote AEM API (GetPlatformWellActual / GetPlatformWellDummy).
/// </summary>
[Table("Platforms")]
public class Platform
{
    [Key]
    public int Id { get; set; }

    [MaxLength(500)]
    public string? UniqueName { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Tracks when this record was last synced from the remote API.
    /// </summary>
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property: Wells belonging to this Platform.
    /// </summary>
    public ICollection<Well> Wells { get; set; } = new List<Well>();
}
