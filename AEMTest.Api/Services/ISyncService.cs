using AEMTest.Api.Models.Dtos;
using AEMTest.Api.Models.Entities;

namespace AEMTest.Api.Services;

/// <summary>
/// Abstraction for the data sync service that upserts Platform and Well data into the database.
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// Runs a full sync using GetPlatformWellActual data.
    /// Upserts (insert or update by Id) Platform and Well records.
    /// </summary>
    Task<SyncResult> SyncActualDataAsync();

    /// <summary>
    /// Runs a sync using GetPlatformWellDummy data.
    /// Handles different/missing keys gracefully — maps only known fields.
    /// </summary>
    Task<SyncResult> SyncDummyDataAsync();
}
