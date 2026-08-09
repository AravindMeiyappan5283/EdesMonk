using Microsoft.Extensions.Logging;
using MonkFruitStore.Application;

namespace MonkFruitStore.Infrastructure;

public sealed class FallbackStoreRepository : IStoreRepository
{
    private readonly IStoreRepository _primary;
    private readonly IStoreRepository _fallback;
    private readonly ILogger<FallbackStoreRepository> _logger;

    public FallbackStoreRepository(
        IStoreRepository primary,
        IStoreRepository fallback,
        ILogger<FallbackStoreRepository> logger)
    {
        _primary = primary;
        _fallback = fallback;
        _logger = logger;
    }

    public async Task<StoreSnapshot> ReadAsync(CancellationToken ct = default)
    {
        try
        {
            return await _primary.ReadAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Primary store is unavailable. Reading from local JSON fallback.");
            return await _fallback.ReadAsync(ct);
        }
    }

    public async Task SaveAsync(StoreSnapshot snapshot, CancellationToken ct = default)
    {
        try
        {
            await _primary.SaveAsync(snapshot, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Primary store is unavailable. Saving to local JSON fallback.");
            await _fallback.SaveAsync(snapshot, ct);
        }
    }
}
