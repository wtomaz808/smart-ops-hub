using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SmartOpsHub.Infrastructure.Data;

/// <summary>
/// Design-time factory for EF Core migrations tooling (dotnet ef migrations add/update).
/// Uses localdb by default; override with ConnectionStrings__SmartOpsHub env var.
/// </summary>
public sealed class SmartOpsHubDbContextFactory : IDesignTimeDbContextFactory<SmartOpsHubDbContext>
{
    public SmartOpsHubDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SmartOpsHub"] =
                    "Server=(localdb)\\mssqllocaldb;Database=SmartOpsHub;Trusted_Connection=true"
            })
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<SmartOpsHubDbContext>();
        optionsBuilder.UseSqlServer(configuration.GetConnectionString("SmartOpsHub"));

        return new SmartOpsHubDbContext(optionsBuilder.Options, configuration);
    }
}
