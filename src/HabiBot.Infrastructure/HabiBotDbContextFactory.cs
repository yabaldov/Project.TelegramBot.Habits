using HabiBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HabiBot.Infrastructure;

/// <summary>
/// Фабрика для создания DbContext во время разработки (для миграций)
/// </summary>
public class HabiBotDbContextFactory : IDesignTimeDbContextFactory<HabiBotDbContext>
{
    public HabiBotDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<HabiBotDbContext>();
        
        // Строка подключения для миграций
        var connectionString = Environment.GetEnvironmentVariable("HABIBOT_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=habibot_dev;Username=postgres;Password=postgres";
        
        optionsBuilder.UseNpgsql(connectionString);

        return new HabiBotDbContext(optionsBuilder.Options);
    }
}
