using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PaxScan.Infrastructure.Persistence;

/// <summary>Used by <c>dotnet ef migrations add</c> only.</summary>
internal sealed class DesignTimeHubDbContextFactory : IDesignTimeDbContextFactory<HubDbContext>
{
    public HubDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<HubDbContext>().UseSqlite("Data Source=design-time.db").Options);
}
