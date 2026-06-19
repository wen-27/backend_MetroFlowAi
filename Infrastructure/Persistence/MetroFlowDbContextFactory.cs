using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Persistence;

public sealed class MetroFlowDbContextFactory : IDesignTimeDbContextFactory<MetroFlowDbContext>
{
    public MetroFlowDbContext CreateDbContext(string[] args)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var apiDirectory = Path.Combine(currentDirectory, "Api");

        if (!Directory.Exists(apiDirectory))
            apiDirectory = Path.GetFullPath(Path.Combine(currentDirectory, "..", "Api"));

        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5433;Database=metroflow_ai;Username=metroflow;Password=metroflow123";

        var options = new DbContextOptionsBuilder<MetroFlowDbContext>()
            .UseNpgsql(
                connectionString,
                postgres => postgres.EnableRetryOnFailure())
            .Options;

        return new MetroFlowDbContext(options);
    }
}
