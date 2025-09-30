using DSharpPlus;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Minefield;
using Minefield.Data;
using Minefield.Services;

namespace MinefieldDev
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var services = new ServiceCollection();

            var root = Directory.GetCurrentDirectory();
            var dotenv = Path.Combine(root, ".env");

            DotEnv.Load(dotenv);

            services.AddDbContext<MinefieldDbContext>(options =>
                options.UseSqlite($"Data Source={Environment.GetEnvironmentVariable("DB_PATH")}")
            );

            DiscordClient client = new DiscordClient(new DiscordConfiguration()
            {
                Token = Environment.GetEnvironmentVariable("BOT_TOKEN"),
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents,
            });

            client.UseInteractivity(new InteractivityConfiguration()
            {
                Timeout = TimeSpan.FromSeconds(30)
            });

            services.AddSingleton(client);

            services.AddScoped<UserService>();
            services.AddScoped<MinefieldService>();
            services.AddScoped<CommandService>();
            services.AddScoped<BotService>();

            var serviceProvider = services.BuildServiceProvider();

            using (var scope = serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MinefieldDbContext>();
                db.Database.EnsureCreated();
            }

            serviceProvider.GetRequiredService<BotService>();

            await client.ConnectAsync();
            await Task.Delay(-1);
        }
    }
}