using AEMTest.Api.Data;
using AEMTest.Api.Models.Dtos;
using AEMTest.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AEMTest.Api.Controllers;

/// <summary>
/// Exposes endpoints to trigger data sync from the remote API and inspect synced data.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SyncController : ControllerBase
{
    private readonly ISyncService _syncService;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<SyncController> _logger;

    public SyncController(
        ISyncService syncService,
        AppDbContext dbContext,
        ILogger<SyncController> logger)
    {
        _syncService = syncService;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Triggers a full sync from GetPlatformWellActual into the local database.
    /// Login is performed automatically using configured credentials.
    /// </summary>
    /// <returns>Summary of inserted and updated records.</returns>
    [HttpPost("run")]
    [ProducesResponseType(typeof(SyncResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SyncResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RunActualSync()
    {
        _logger.LogInformation("POST /api/sync/run triggered.");
        var result = await _syncService.SyncActualDataAsync();
        return result.Success ? Ok(result) : StatusCode(500, result);
    }

    /// <summary>
    /// Triggers a sync from GetPlatformWellDummy into the local database.
    /// Tests resilient deserialization — handles different/missing JSON keys gracefully.
    /// </summary>
    /// <returns>Summary of inserted and updated records.</returns>
    [HttpPost("run-dummy")]
    [ProducesResponseType(typeof(SyncResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SyncResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RunDummySync()
    {
        _logger.LogInformation("POST /api/sync/run-dummy triggered.");
        var result = await _syncService.SyncDummyDataAsync();
        return result.Success ? Ok(result) : StatusCode(500, result);
    }

    /// <summary>
    /// Returns all Platform records currently stored in the local database.
    /// </summary>
    [HttpGet("platforms")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlatforms()
    {
        var platforms = await _dbContext.Platforms
            .OrderBy(p => p.Id)
            .Select(p => new
            {
                p.Id,
                p.UniqueName,
                p.Latitude,
                p.Longitude,
                p.CreatedAt,
                p.UpdatedAt,
                p.SyncedAt,
                WellCount = p.Wells.Count
            })
            .ToListAsync();

        return Ok(platforms);
    }

    /// <summary>
    /// Returns all Well records for a given Platform ID.
    /// </summary>
    /// <param name="id">The Platform ID.</param>
    [HttpGet("platforms/{id}/wells")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWellsByPlatform(int id)
    {
        var platform = await _dbContext.Platforms.FindAsync(id);
        if (platform is null)
            return NotFound(new { message = $"Platform with Id={id} not found." });

        var wells = await _dbContext.Wells
            .Where(w => w.PlatformId == id)
            .OrderBy(w => w.Id)
            .Select(w => new
            {
                w.Id,
                w.UniqueName,
                w.Latitude,
                w.Longitude,
                w.CreatedAt,
                w.UpdatedAt,
                w.PlatformId,
                w.SyncedAt
            })
            .ToListAsync();

        return Ok(wells);
    }

    /// <summary>
    /// Returns all Well records from the local database.
    /// </summary>
    [HttpGet("wells")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllWells()
    {
        var wells = await _dbContext.Wells
            .OrderBy(w => w.Id)
            .Select(w => new
            {
                w.Id,
                w.UniqueName,
                w.Latitude,
                w.Longitude,
                w.CreatedAt,
                w.UpdatedAt,
                w.PlatformId,
                w.SyncedAt
            })
            .ToListAsync();

        return Ok(wells);
    }
}
