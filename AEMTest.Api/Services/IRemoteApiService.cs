using AEMTest.Api.Models.Dtos;

namespace AEMTest.Api.Services;

/// <summary>
/// Abstraction for communicating with the remote AEM REST API.
/// </summary>
public interface IRemoteApiService
{
    /// <summary>
    /// Authenticates with the remote API and returns a bearer token.
    /// Returns null if authentication fails.
    /// </summary>
    Task<string?> LoginAsync();

    /// <summary>
    /// Fetches the actual platform/well data using the given bearer token.
    /// </summary>
    Task<List<PlatformDto>> GetPlatformWellActualAsync(string bearerToken);

    /// <summary>
    /// Fetches the dummy platform/well data using the given bearer token.
    /// Dummy data may have different/missing keys — should be handled gracefully.
    /// </summary>
    Task<List<PlatformDummyDto>> GetPlatformWellDummyAsync(string bearerToken);
}
