using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TP04.EntityFrameworkCore;

/* This class is needed for EF Core console commands
 * (like Add-Migration and Update-Database commands) */
public class TP04DbContextFactory : IDesignTimeDbContextFactory<TP04DbContext>
{
    public TP04DbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();
        
        TP04EfCoreEntityExtensionMappings.Configure();

        var builder = new DbContextOptionsBuilder<TP04DbContext>()
            .UseSqlServer(configuration.GetConnectionString("Default"));
        
        return new TP04DbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../TP04.DbMigrator/"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables();

        return builder.Build();
    }
}
