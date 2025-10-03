using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Minefield;
using Minefield.Commands;
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

            services.AddScoped<EmbedService>();
            services.AddScoped<UserService>();
            services.AddScoped<MinefieldService>();
            services.AddScoped<BotService>();

            services.AddSingleton(client);

            var serviceProvider = services.BuildServiceProvider();

            using (var scope = serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MinefieldDbContext>();
                db.Database.EnsureCreated();
            }

            serviceProvider.GetRequiredService<BotService>();

            CommandsNextExtension commands = client.UseCommandsNext(new CommandsNextConfiguration()
            {
                EnableDefaultHelp = false,
                EnableMentionPrefix = false,
                EnableDms = false,
                StringPrefixes = ["!"],
                Services = serviceProvider
            });

            commands.RegisterCommands<EventCommands>();
            commands.RegisterCommands<PerkCommands>();
            commands.RegisterCommands<UtilityCommands>();

            commands.CommandErrored += async (s, e) =>
            {
                if (e.Exception is ArgumentException)
                {
                    await e.Context.RespondAsync("Invalid arguments.");
                }
                else if (e.Exception is CommandNotFoundException)
                {
                    await e.Context.RespondAsync("Unknown command. Try \"!help\".");
                }
                else if (e.Exception is ChecksFailedException)
                {
                    await e.Context.RespondAsync("You are unable to send this command.");
                }
                else
                {
                    await e.Context.RespondAsync($"Error: {e.Exception.Message}");
                }
            };

            await client.ConnectAsync();
            await Task.Delay(-1);
        }
    }
}