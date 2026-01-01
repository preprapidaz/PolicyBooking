using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PolicyService.Infrastructure.Persistence
{
    /// <summary>
    /// Design-time factory for EF Core migrations
    /// Uses hardcoded connection string
    /// </summary>
    public class PolicyDbContextFactory : IDesignTimeDbContextFactory<PolicyDbContext>
    {
        public PolicyDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PolicyDbContext>();

            // Hardcoded connection string - replace YOUR_PASSWORD with actual password
            optionsBuilder.UseSqlServer(
                "Server=tcp:sqlserver-azlearning-westus.database.windows.net,1433;Initial Catalog=azlearningdb;User ID=abdul;Password=Mastan@01;Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;",
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly("PolicyService.Infrastructure");
                });

            return new PolicyDbContext(optionsBuilder.Options);
        }
    }
}