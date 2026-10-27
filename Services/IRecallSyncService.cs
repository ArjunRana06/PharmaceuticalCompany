using Project.Models;

namespace Project.Services;

public interface IRecallSyncService
{
    Task<int> SyncLatestAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecallFeedItem>> FetchLatestAsync(CancellationToken cancellationToken = default);
}
