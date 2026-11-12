using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Cloud.Firestore;
using MonkFruitStore.Application;

namespace MonkFruitStore.Infrastructure;

public sealed class FirestoreStoreRepository : IStoreRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly DocumentReference _document;
    private readonly string? _seedSnapshotPath;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public FirestoreStoreRepository(string projectId, string collectionName, string documentId, string? seedSnapshotPath = null)
    {
        if (string.IsNullOrWhiteSpace(projectId))
        {
            throw new InvalidOperationException("Firebase project id is required.");
        }

        var db = FirestoreDb.Create(projectId.Trim());
        _document = db.Collection(collectionName).Document(documentId);
        _seedSnapshotPath = seedSnapshotPath;
    }

    public async Task<StoreSnapshot> ReadAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var snapshot = await _document.GetSnapshotAsync(ct);
            if (!snapshot.Exists || !snapshot.ContainsField("snapshotJson"))
            {
                var seed = await LoadInitialSnapshotAsync(ct);
                await SaveUnsafeAsync(seed, ct);
                return seed;
            }

            var json = snapshot.GetValue<string>("snapshotJson");
            if (string.IsNullOrWhiteSpace(json))
            {
                return JsonStoreRepository.CreateSeed();
            }

            return JsonSerializer.Deserialize<StoreSnapshot>(StoreSnapshotText.Clean(json), JsonOptions) ?? JsonStoreRepository.CreateSeed();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(StoreSnapshot snapshot, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            await SaveUnsafeAsync(snapshot, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task SaveUnsafeAsync(StoreSnapshot snapshot, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(snapshot, JsonOptions);
        await _document.SetAsync(new Dictionary<string, object>
        {
            ["snapshotJson"] = json,
            ["updatedAtUtc"] = DateTimeOffset.UtcNow.UtcDateTime,
            ["schema"] = "StoreSnapshot:v1"
        }, SetOptions.Overwrite, ct);
    }

    private async Task<StoreSnapshot> LoadInitialSnapshotAsync(CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(_seedSnapshotPath))
        {
            var localSnapshot = await JsonStoreRepository.TryReadSnapshotFileAsync(_seedSnapshotPath, ct);
            if (localSnapshot is not null)
            {
                return localSnapshot;
            }
        }

        return JsonStoreRepository.CreateSeed();
    }
}
