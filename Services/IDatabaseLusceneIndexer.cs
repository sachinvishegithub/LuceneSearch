using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LuceneSearchHelper.Services;

public interface IDatabaseLusceneIndexer
{
    Task ReindexAllAsync(CancellationToken cancellationToken = default);
}
