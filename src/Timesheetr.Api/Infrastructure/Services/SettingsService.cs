using Microsoft.EntityFrameworkCore;
using Timesheetr.Api.Infrastructure.Data;
using Timesheetr.Api.Infrastructure.Models;

namespace Timesheetr.Api.Infrastructure.Services;

public class SettingsService
{
    private readonly IDbContextFactory<TimesheetrDbContext> _dbFactory;
    private readonly IConfiguration _configuration;
    private AppSettings? _cache;

    public SettingsService(IDbContextFactory<TimesheetrDbContext> dbFactory, IConfiguration configuration)
    {
        _dbFactory = dbFactory;
        _configuration = configuration;

        using var db = _dbFactory.CreateDbContext();
        db.Database.Migrate();
    }

    public AppSettings Settings => _cache ??= Load();

    public void Save(AppSettings settings)
    {
        using var db = _dbFactory.CreateDbContext();

        var entity = db.Settings.Find(1);
        if (entity is null)
        {
            entity = new SettingsEntity { Id = 1 };
            db.Settings.Add(entity);
        }

        entity.TogglApiToken = settings.TogglApiToken;
        entity.TempoApiToken = settings.TempoApiToken;
        entity.JiraAccountId = settings.JiraAccountId;
        entity.JiraBaseUrl = settings.JiraBaseUrl;
        entity.JiraEmail = settings.JiraEmail;
        entity.JiraApiToken = settings.JiraApiToken;

        db.SaveChanges();
        _cache = settings;
    }

    private AppSettings Load()
    {
        using var db = _dbFactory.CreateDbContext();
        var entity = db.Settings.AsNoTracking().SingleOrDefault();

        if (entity is not null)
        {
            return new AppSettings
            {
                TogglApiToken = entity.TogglApiToken,
                TempoApiToken = entity.TempoApiToken,
                JiraAccountId = entity.JiraAccountId,
                JiraBaseUrl = entity.JiraBaseUrl,
                JiraEmail = entity.JiraEmail,
                JiraApiToken = entity.JiraApiToken,
            };
        }

        var section = _configuration.GetSection("Timesheetr");
        return new AppSettings
        {
            TogglApiToken = section["TogglApiToken"] ?? string.Empty,
            TempoApiToken = section["TempoApiToken"] ?? string.Empty,
            JiraAccountId = section["JiraAccountId"] ?? string.Empty,
            JiraBaseUrl = section["JiraBaseUrl"] ?? string.Empty,
            JiraEmail = section["JiraEmail"] ?? string.Empty,
            JiraApiToken = section["JiraApiToken"] ?? string.Empty,
        };
    }
}
