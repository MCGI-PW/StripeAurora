using AuroraPet.Payments.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuroraPet.Infrastructure.Data;

public class AuroraDbContext : DbContext
{
    public AuroraDbContext(DbContextOptions<AuroraDbContext> options) : base(options)
    {
    }

    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<TutorSubscription> TutorSubscriptions => Set<TutorSubscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuroraDbContext).Assembly);
    }
}
