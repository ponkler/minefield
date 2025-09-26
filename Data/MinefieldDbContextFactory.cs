using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Minefield.Data
{
    public class MinefieldDbContextFactory : IDesignTimeDbContextFactory<MinefieldDbContext>
    {
        public MinefieldDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<MinefieldDbContext>();
            optionsBuilder.UseSqlite("Data Source=minefield.db");

            return new MinefieldDbContext(optionsBuilder.Options);
        }
    }
}