using AEMTest.Api.Data;
using AEMTest.Api.Models.Dtos;
using AEMTest.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AEMTest.Api.Services;

/// <summary>
/// Orchestrates the sync workflow:
///   1. Login to remote API to obtain bearer token
///   2. Fetch platform/well data
///   3. Upsert into local database (insert if new ID, update if existing ID)
/// </summary>
public class SyncService : ISyncService
{
    private readonly IRemoteApiService _remoteApiService;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<SyncService> _logger;

    public SyncService(
        IRemoteApiService remoteApiService,
        AppDbContext dbContext,
        ILogger<SyncService> logger)
    {
        _remoteApiService = remoteApiService;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<SyncResult> SyncActualDataAsync()
    {
        var result = new SyncResult { SyncedAt = DateTime.UtcNow };

        try
        {
            // Step 1: Login and acquire token
            var token = await _remoteApiService.LoginAsync();
            if (string.IsNullOrWhiteSpace(token))
            {
                result.Success = false;
                result.Message = "Authentication failed. Could not obtain bearer token.";
                return result;
            }

            // Step 2: Fetch actual platform/well data
            var platforms = await _remoteApiService.GetPlatformWellActualAsync(token);

            // Step 3: Upsert each platform and its nested wells
            foreach (var platformDto in platforms)
            {
                if (platformDto.Id is null)
                {
                    _logger.LogWarning("Skipping platform with null Id.");
                    continue;
                }

                var (platformInserted, platformUpdated) = await UpsertPlatformAsync(platformDto);
                result.PlatformsInserted += platformInserted;
                result.PlatformsUpdated += platformUpdated;

                if (platformDto.Wells is not null)
                {
                    foreach (var wellDto in platformDto.Wells)
                    {
                        if (wellDto.Id is null)
                        {
                            _logger.LogWarning("Skipping well with null Id under platform {PlatformId}.", platformDto.Id);
                            continue;
                        }

                        // Use the well's own platformId, or fall back to the parent platform's Id
                        var effectivePlatformId = wellDto.PlatformId ?? platformDto.Id.Value;
                        var (wellInserted, wellUpdated) = await UpsertWellFromActualAsync(wellDto, effectivePlatformId);
                        result.WellsInserted += wellInserted;
                        result.WellsUpdated += wellUpdated;
                    }
                }
            }

            await _dbContext.SaveChangesAsync();

            result.Success = true;
            result.Message = $"Sync completed. Platforms: +{result.PlatformsInserted} inserted, ~{result.PlatformsUpdated} updated. " +
                             $"Wells: +{result.WellsInserted} inserted, ~{result.WellsUpdated} updated.";

            _logger.LogInformation(result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error during SyncActualDataAsync.");
            result.Success = false;
            result.Message = "Sync failed due to an unexpected error.";
            result.ErrorDetail = ex.Message;
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<SyncResult> SyncDummyDataAsync()
    {
        var result = new SyncResult { SyncedAt = DateTime.UtcNow };

        try
        {
            // Step 1: Login and acquire token
            var token = await _remoteApiService.LoginAsync();
            if (string.IsNullOrWhiteSpace(token))
            {
                result.Success = false;
                result.Message = "Authentication failed. Could not obtain bearer token.";
                return result;
            }

            // Step 2: Fetch dummy data (uses different JSON keys — lastUpdate instead of createdAt/updatedAt)
            var platforms = await _remoteApiService.GetPlatformWellDummyAsync(token);

            // Step 3: Upsert using only fields that exist in the dummy schema
            foreach (var platformDto in platforms)
            {
                if (platformDto.Id is null)
                {
                    _logger.LogWarning("Skipping dummy platform with null Id.");
                    continue;
                }

                var (platformInserted, platformUpdated) = await UpsertPlatformFromDummyAsync(platformDto);
                result.PlatformsInserted += platformInserted;
                result.PlatformsUpdated += platformUpdated;

                if (platformDto.Wells is not null)
                {
                    foreach (var wellDto in platformDto.Wells)
                    {
                        if (wellDto.Id is null)
                        {
                            _logger.LogWarning("Skipping dummy well with null Id under platform {PlatformId}.", platformDto.Id);
                            continue;
                        }

                        var effectivePlatformId = wellDto.PlatformId ?? platformDto.Id.Value;
                        var (wellInserted, wellUpdated) = await UpsertWellFromDummyAsync(wellDto, effectivePlatformId);
                        result.WellsInserted += wellInserted;
                        result.WellsUpdated += wellUpdated;
                    }
                }
            }

            await _dbContext.SaveChangesAsync();

            result.Success = true;
            result.Message = $"Dummy sync completed. Platforms: +{result.PlatformsInserted} inserted, ~{result.PlatformsUpdated} updated. " +
                             $"Wells: +{result.WellsInserted} inserted, ~{result.WellsUpdated} updated.";

            _logger.LogInformation(result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error during SyncDummyDataAsync.");
            result.Success = false;
            result.Message = "Dummy sync failed due to an unexpected error.";
            result.ErrorDetail = ex.Message;
        }

        return result;
    }

    // ─── Private Helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Upserts a Platform from the Actual API DTO.
    /// Returns (inserted, updated) counts — each 0 or 1.
    /// </summary>
    private async Task<(int inserted, int updated)> UpsertPlatformAsync(PlatformDto dto)
    {
        var existing = await _dbContext.Platforms.FindAsync(dto.Id!.Value);

        if (existing is null)
        {
            // INSERT new platform
            var platform = new Platform
            {
                Id = dto.Id.Value,
                UniqueName = dto.UniqueName,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt,
                SyncedAt = DateTime.UtcNow
            };
            await _dbContext.Platforms.AddAsync(platform);
            _logger.LogDebug("INSERT Platform Id={Id}, UniqueName={Name}", dto.Id.Value, dto.UniqueName);
            return (1, 0);
        }
        else
        {
            // UPDATE: only overwrite fields that are non-null in the DTO, preserving existing data
            existing.UniqueName = dto.UniqueName ?? existing.UniqueName;
            existing.Latitude = dto.Latitude ?? existing.Latitude;
            existing.Longitude = dto.Longitude ?? existing.Longitude;
            existing.CreatedAt = dto.CreatedAt ?? existing.CreatedAt;
            existing.UpdatedAt = dto.UpdatedAt ?? existing.UpdatedAt;
            existing.SyncedAt = DateTime.UtcNow;
            _dbContext.Platforms.Update(existing);
            _logger.LogDebug("UPDATE Platform Id={Id}, UniqueName={Name}", dto.Id.Value, dto.UniqueName);
            return (0, 1);
        }
    }

    /// <summary>
    /// Upserts a Platform from the Dummy API DTO.
    /// Maps known fields only — lastUpdate is used when createdAt/updatedAt are absent.
    /// </summary>
    private async Task<(int inserted, int updated)> UpsertPlatformFromDummyAsync(PlatformDummyDto dto)
    {
        var existing = await _dbContext.Platforms.FindAsync(dto.Id!.Value);

        if (existing is null)
        {
            var platform = new Platform
            {
                Id = dto.Id.Value,
                UniqueName = dto.UniqueName,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                // Fall back to lastUpdate if createdAt/updatedAt are missing (Dummy API pattern)
                CreatedAt = dto.CreatedAt ?? dto.LastUpdate,
                UpdatedAt = dto.UpdatedAt ?? dto.LastUpdate,
                SyncedAt = DateTime.UtcNow
            };
            await _dbContext.Platforms.AddAsync(platform);
            _logger.LogDebug("INSERT Platform (dummy) Id={Id}, UniqueName={Name}", dto.Id.Value, dto.UniqueName);
            return (1, 0);
        }
        else
        {
            existing.UniqueName = dto.UniqueName ?? existing.UniqueName;
            existing.Latitude = dto.Latitude ?? existing.Latitude;
            existing.Longitude = dto.Longitude ?? existing.Longitude;
            existing.CreatedAt = dto.CreatedAt ?? dto.LastUpdate ?? existing.CreatedAt;
            existing.UpdatedAt = dto.UpdatedAt ?? dto.LastUpdate ?? existing.UpdatedAt;
            existing.SyncedAt = DateTime.UtcNow;
            _dbContext.Platforms.Update(existing);
            _logger.LogDebug("UPDATE Platform (dummy) Id={Id}", dto.Id.Value);
            return (0, 1);
        }
    }

    /// <summary>
    /// Upserts a Well from the Actual API DTO.
    /// </summary>
    private async Task<(int inserted, int updated)> UpsertWellFromActualAsync(WellDto dto, int platformId)
    {
        var existing = await _dbContext.Wells.FindAsync(dto.Id!.Value);

        if (existing is null)
        {
            var well = new Well
            {
                Id = dto.Id.Value,
                UniqueName = dto.UniqueName,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt,
                PlatformId = platformId,
                SyncedAt = DateTime.UtcNow
            };
            await _dbContext.Wells.AddAsync(well);
            _logger.LogDebug("INSERT Well Id={Id}, UniqueName={Name} under Platform {PlatformId}", dto.Id.Value, dto.UniqueName, platformId);
            return (1, 0);
        }
        else
        {
            existing.UniqueName = dto.UniqueName ?? existing.UniqueName;
            existing.Latitude = dto.Latitude ?? existing.Latitude;
            existing.Longitude = dto.Longitude ?? existing.Longitude;
            existing.CreatedAt = dto.CreatedAt ?? existing.CreatedAt;
            existing.UpdatedAt = dto.UpdatedAt ?? existing.UpdatedAt;
            existing.PlatformId = platformId;
            existing.SyncedAt = DateTime.UtcNow;
            _dbContext.Wells.Update(existing);
            _logger.LogDebug("UPDATE Well Id={Id}", dto.Id.Value);
            return (0, 1);
        }
    }

    /// <summary>
    /// Upserts a Well from the Dummy API DTO.
    /// Uses lastUpdate as fallback for missing createdAt/updatedAt.
    /// </summary>
    private async Task<(int inserted, int updated)> UpsertWellFromDummyAsync(WellDummyDto dto, int platformId)
    {
        var existing = await _dbContext.Wells.FindAsync(dto.Id!.Value);

        if (existing is null)
        {
            var well = new Well
            {
                Id = dto.Id.Value,
                UniqueName = dto.UniqueName,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                CreatedAt = dto.CreatedAt ?? dto.LastUpdate,
                UpdatedAt = dto.UpdatedAt ?? dto.LastUpdate,
                PlatformId = platformId,
                SyncedAt = DateTime.UtcNow
            };
            await _dbContext.Wells.AddAsync(well);
            _logger.LogDebug("INSERT Well (dummy) Id={Id}, UniqueName={Name} under Platform {PlatformId}", dto.Id.Value, dto.UniqueName, platformId);
            return (1, 0);
        }
        else
        {
            existing.UniqueName = dto.UniqueName ?? existing.UniqueName;
            existing.Latitude = dto.Latitude ?? existing.Latitude;
            existing.Longitude = dto.Longitude ?? existing.Longitude;
            existing.CreatedAt = dto.CreatedAt ?? dto.LastUpdate ?? existing.CreatedAt;
            existing.UpdatedAt = dto.UpdatedAt ?? dto.LastUpdate ?? existing.UpdatedAt;
            existing.PlatformId = platformId;
            existing.SyncedAt = DateTime.UtcNow;
            _dbContext.Wells.Update(existing);
            _logger.LogDebug("UPDATE Well (dummy) Id={Id}", dto.Id.Value);
            return (0, 1);
        }
    }
}
