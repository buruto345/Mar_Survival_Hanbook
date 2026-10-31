using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Mars_Survival_Hanbook.Data;

/// <summary>Creates the context for both dotnet ef and Visual Studio Package Manager Console.</summary>
public sealed class MarsDbContextFactory : IDesignTimeDbContextFactory<MarsDbContext>
{
    public MarsDbContext CreateDbContext(string[] args)
    {
        // Read the startup project's copied configuration independently of the shell's directory.
        // CreateBuilder retains the usual appsettings, environment, user secrets and CLI precedence.
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ApplicationName = typeof(MarsDbContextFactory).Assembly.GetName().Name,
            ContentRootPath = Path.GetDirectoryName(typeof(MarsDbContextFactory).Assembly.Location)
        });
        var connectionString = builder.Configuration.GetConnectionString("MarsDatabase")
            ?? throw new InvalidOperationException("Connection string 'MarsDatabase' is not configured.");

        return new MarsDbContext(new DbContextOptionsBuilder<MarsDbContext>()
            .UseSqlServer(connectionString)
            .Options);
    }
}
