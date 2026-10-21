using Microsoft.EntityFrameworkCore;
using PaxScan.Application.Abstractions;

namespace PaxScan.Infrastructure.Persistence;

internal sealed class HubDbContextFactory(IDbContextFactory<HubDbContext> factory) : IHubDbContextFactory
{
    public async Task<IHubDbContext> CreateAsync(CancellationToken cancellationToken = default) =>
        await factory.CreateDbContextAsync(cancellationToken);
}
