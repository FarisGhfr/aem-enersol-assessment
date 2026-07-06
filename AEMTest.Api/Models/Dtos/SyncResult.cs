namespace AEMTest.Api.Models.Dtos;

/// <summary>
/// Result summary returned after a sync operation.
/// </summary>
public class SyncResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int PlatformsInserted { get; set; }
    public int PlatformsUpdated { get; set; }
    public int WellsInserted { get; set; }
    public int WellsUpdated { get; set; }
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
    public string? ErrorDetail { get; set; }
}
