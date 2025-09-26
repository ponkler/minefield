using Microsoft.EntityFrameworkCore;
using Minefield.Entities;

namespace Minefield.Data
{
    public class MinefieldDbContext : DbContext
    {
        public MinefieldDbContext(DbContextOptions<MinefieldDbContext> options) : base(options) { }

        public DbSet<MinefieldUser> Users { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<MinefieldUser>()
                .HasKey(u => new { u.UserId, u.ServerId });

            modelBuilder.Entity<MinefieldUser>()
                .HasOne(u => u.LifelineTarget)
                .WithOne(u => u.LifelineProvider)
                .HasForeignKey<MinefieldUser>(u => new { u.LifelineTargetId, u.LifelineTargetServerId })
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MinefieldUser>()
                .HasOne(u => u.SacrificeTarget)
                .WithOne(u => u.SacrificeProvider)
                .HasForeignKey<MinefieldUser>(u => new { u.SacrificeTargetId, u.SacrificeTargetServerId })
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MinefieldUser>()
                .HasOne(u => u.SymbioteTarget)
                .WithOne(u => u.SymbioteProvider)
                .HasForeignKey<MinefieldUser>(u => new { u.SymbioteTargetId, u.SymbioteTargetServerId })
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
