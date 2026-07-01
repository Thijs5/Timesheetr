using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Timesheetr.Api.Infrastructure.Data;

public class TimesheetrDbContext(DbContextOptions<TimesheetrDbContext> options) : DbContext(options)
{
    public DbSet<SettingsEntity> Settings { get; set; }
    public DbSet<SyncEntryStateEntity> SyncEntryStates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}

public class SettingsEntity
{
    public int Id { get; set; } = 1;
    public string TogglApiToken { get; set; } = "";
    public string TempoApiToken { get; set; } = "";
    public string JiraAccountId { get; set; } = "";
    public string JiraBaseUrl { get; set; } = "";
    public string JiraEmail { get; set; } = "";
    public string JiraApiToken { get; set; } = "";
}

public class SettingsEntityConfiguration : IEntityTypeConfiguration<SettingsEntity>
{
    public void Configure(EntityTypeBuilder<SettingsEntity> builder)
    {
        builder.ToTable("Settings", t => t.HasCheckConstraint("CK_Settings_SingleRow", "Id = 1"));
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
    }
}
