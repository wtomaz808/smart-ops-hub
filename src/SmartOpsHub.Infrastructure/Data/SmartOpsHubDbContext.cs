using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace SmartOpsHub.Infrastructure.Data;

public sealed class SmartOpsHubDbContext : DbContext
{
    private readonly IConfiguration _configuration;

    public SmartOpsHubDbContext(DbContextOptions<SmartOpsHubDbContext> options, IConfiguration configuration)
        : base(options)
    {
        _configuration = configuration;
    }

    public DbSet<ConversationLog> ConversationLogs => Set<ConversationLog>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var connectionString = _configuration.GetConnectionString("SmartOpsHub");
            optionsBuilder.UseSqlServer(connectionString);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ConversationLog>(entity =>
        {
            entity.ToTable("ConversationLogs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.Property(e => e.UserId).HasMaxLength(256).IsRequired();
            entity.Property(e => e.AgentType).HasMaxLength(64).IsRequired();
            entity.Property(e => e.MessageContent).IsRequired();
            entity.Property(e => e.Role).HasMaxLength(32).IsRequired();
            entity.Property(e => e.Timestamp).IsRequired();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Timestamp);
        });
    }
}
