namespace PaxScan.Application.Abstractions;

/// <summary>
/// Creates a short-lived context per operation. A DbContext is not thread-safe, and handlers are
/// called from both the WPF UI and concurrent API requests.
/// </summary>
public interface IHubDbContextFactory
{
    Task<IHubDbContext> CreateAsync(CancellationToken cancellationToken = default);
}
