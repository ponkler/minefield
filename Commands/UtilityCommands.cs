using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Minefield.Services;

namespace Minefield.Commands
{
    public class UtilityCommands : BaseCommandModule
    {
        private readonly MinefieldService _minefieldService;
        private readonly UserService _userService;

        private readonly EmbedService _embedService;

        public UtilityCommands(MinefieldService minefieldService, UserService userService, EmbedService embedService)
        {
            _minefieldService = minefieldService;
            _userService = userService;
            _embedService = embedService;
        }

        [Command("balance")]
        public async Task Balance(CommandContext ctx)
        {
            var user = await _userService.GetOrCreateUserAsync(ctx.User.Id, ctx.Guild.Id, ctx.User.Username);

            await ctx.RespondAsync($"{Formatter.Sanitize(user.Username)} has {user.Currency:N0} MF$.");
        }

        [Command("balance")]
        public async Task Balance(CommandContext ctx, string username)
        {
            var user = await _userService.GetUserByUsernameAsync(username, ctx.Guild.Id);

            if (user == null) 
            {
                await ctx.RespondAsync($"Could not find user {username}");
                return;
            }

            await ctx.RespondAsync($"{Formatter.Sanitize(user.Username)} has {user.Currency:N0} MF$.");
        }

        [Command("cooldowns")]
        public async Task Cooldowns(CommandContext ctx)
        {
            var user = await _userService.GetOrCreateUserAsync(ctx.User.Id, ctx.Guild.Id, ctx.User.Username);

            await _embedService.SendCooldownEmbedAsync(ctx, user);
        }

        [Command("deadusers"), Aliases("dead")]
        public async Task DeadUsers(CommandContext ctx) => await _embedService.SendDeadUsersEmbedAsync(ctx);

        [Command("help")]
        public async Task Help(CommandContext ctx) => await _embedService.SendHelpEmbedAsync(ctx);

        [Command("immune"), RequireRoles(RoleCheckMode.MatchNames, "Minefield Janitor")]
        public async Task Immune(CommandContext ctx)
        {
            var user = await _userService.GetOrCreateUserAsync(ctx.User.Id, ctx.Guild.Id, ctx.User.Username);

            await _minefieldService.ToggleImmunity(user);

            if (user.IsImmune)
            {
                await ctx.RespondAsync($"You are now immune.");
            }
            else
            {
                await ctx.RespondAsync($"You are no longer immune.");
            }
        }

        [Command("info")]
        public async Task Info(CommandContext ctx) => await _embedService.SendInfoEmbedAsync(ctx);

        [Command("leaderboard")]
        public async Task Leaderboard(CommandContext ctx)
        {
            var leaderboard = await _userService.GetLeaderboardAsync(ctx.Guild.Id);
            List<LeaderboardEntry> entries = new List<LeaderboardEntry>();

            foreach (var entry in leaderboard)
            {
                LeaderboardEntry newEntry = new LeaderboardEntry
                {
                    Name = Formatter.Sanitize(entry.Username),
                    Currency = entry.LifetimeCurrency
                };

                entries.Add(newEntry);
            }

            await _embedService.SendLeaderboardEmbedAsync(ctx, entries);
        }

        [Command("maxodds")]
        public async Task MaxOdds(CommandContext ctx)
        {
            var user = await _userService.GetOrCreateUserAsync(ctx.User.Id, ctx.Guild.Id, ctx.User.Username);

            await ctx.RespondAsync($"{Formatter.Sanitize(user.Username)}'s max odds are 1 in {user.MaxOdds}.");
        }

        [Command("maxodds")]
        public async Task MaxOdds(CommandContext ctx, string username)
        {
            var user = await _userService.GetUserByUsernameAsync(username, ctx.Guild.Id);

            if (user == null)
            {
                await ctx.RespondAsync($"Could not find user {username}");
                return;
            }

            await ctx.RespondAsync($"{Formatter.Sanitize(user.Username)}'s max odds are 1 in {user.MaxOdds}.");
        }

        [Command("odds")]
        public async Task Odds(CommandContext ctx)
        {
            var user = await _userService.GetOrCreateUserAsync(ctx.User.Id, ctx.Guild.Id, ctx.User.Username);

            await ctx.RespondAsync($"{Formatter.Sanitize(user.Username)}'s odds are 1 in {user.CurrentOdds}.");
        }

        [Command("odds")]
        public async Task Odds(CommandContext ctx, string username)
        {
            var user = await _userService.GetUserByUsernameAsync(username, ctx.Guild.Id);

            if (user == null)
            {
                await ctx.RespondAsync($"Could not find user {username}");
                return;
            }

            await ctx.RespondAsync($"{Formatter.Sanitize(user.Username)}'s odds are 1 in {user.CurrentOdds}.");
        }

        [Command("perks")]
        public async Task Perks(CommandContext ctx) => await _embedService.SendPerkEmbedAsync(ctx);

        [Command("reset"), RequireRoles(RoleCheckMode.MatchNames, "Minefield Janitor")]
        public async Task Reset(CommandContext ctx)
        {
            var user = await _userService.GetOrCreateUserAsync(ctx.User.Id, ctx.Guild.Id, ctx.User.Username);

            await _userService.ResetUserAsync(user.UserId, user.ServerId);
            await ctx.RespondAsync($"{Formatter.Sanitize(user.Username)}'s progress has been reset.");
        }

        [Command("reset"), RequireRoles(RoleCheckMode.MatchNames, "Minefield Janitor")]
        public async Task Reset(CommandContext ctx, string username)
        {
            var user = await _userService.GetUserByUsernameAsync(username, ctx.Guild.Id);

            if (user == null)
            {
                await ctx.RespondAsync($"Could not find user {username}");
                return;
            }

            await _userService.ResetUserAsync(user.UserId, user.ServerId);
            await ctx.RespondAsync($"{Formatter.Sanitize(user.Username)}'s progress has been reset.");
        }

        [Command("revive"), RequireRoles(RoleCheckMode.MatchNames, "Minefield Janitor")]
        public async Task Revive(CommandContext ctx)
        {
            var user = await _userService.GetOrCreateUserAsync(ctx.User.Id, ctx.Guild.Id, ctx.User.Username);

            if (user.IsAlive)
            {
                await ctx.RespondAsync("That user is already alive.");
                return;
            }

            _minefieldService.ReviveUser(user);
            await ctx.RespondAsync($"{Formatter.Sanitize(user.Username)} has been revived.");
        }

        [Command("revive"), RequireRoles(RoleCheckMode.MatchNames, "Minefield Janitor")]
        public async Task Revive(CommandContext ctx, string username)
        {
            var user = await _userService.GetUserByUsernameAsync(username, ctx.Guild.Id);

            if (user == null)
            {
                await ctx.RespondAsync($"Could not find user {username}");
                return;
            }

            if (user.IsAlive)
            {
                await ctx.RespondAsync("That user is already alive.");
                return;
            }

            _minefieldService.ReviveUser(user);
            await ctx.RespondAsync($"{Formatter.Sanitize(user.Username)} has been revived.");
        }

        [Command("role")]
        public async Task Role(CommandContext ctx)
        {
            await ctx.RespondAsync("You must enter the name of a role. Use \"!roles\" to see all valid role names.");
            return;
        }

        [Command("role")]
        public async Task Role(CommandContext ctx, string name)
        {
            var arenaRole = ctx.Guild.Roles.Select(r => r.Value).Where(u => u.Name == "Arena").First();
            var cofferRole = ctx.Guild.Roles.Select(r => r.Value).Where(u => u.Name == "Coffer").First();

            switch (name)
            {
                case "Arena":
                    {
                        if (ctx.Member!.Roles.Any(r => r.Name == "Arena")) 
                        {
                            await ctx.Member.RevokeRoleAsync(arenaRole);
                            await ctx.RespondAsync("\"Arena\" role revoked.");
                            return;
                        }

                        await ctx.Member.GrantRoleAsync(arenaRole);
                        await ctx.RespondAsync("\"Arena\" role granted.");
                        return;
                    }
                case "Coffer":
                    {
                        if (ctx.Member!.Roles.Any(r => r.Name == "Coffer"))
                        {
                            await ctx.Member.RevokeRoleAsync(cofferRole);
                            await ctx.RespondAsync("\"Coffer\" role revoked.");
                            return;
                        }

                        await ctx.Member.GrantRoleAsync(cofferRole);
                        await ctx.RespondAsync("\"Coffer\" role granted.");
                        return;
                    } 
            }
        }

        [Command("roles")]
        public async Task Roles(CommandContext ctx)
        {
            await ctx.RespondAsync($"Available roles: \"Arena\", \"Coffer\"");
        }

        [Command("setbalance"), RequireRoles(RoleCheckMode.MatchNames, "Minefield Janitor")]
        public async Task SetBalance(CommandContext ctx, int balance)
        {
            var user = await _userService.GetOrCreateUserAsync(ctx.User.Id, ctx.Guild.Id, ctx.User.Username);

            if (balance < 0)
            {
                await ctx.RespondAsync("Balance cannot be negative.");
                return;
            }

            user.Currency = balance;

            await _userService.SaveAsync();
            await ctx.RespondAsync($"Set {Formatter.Sanitize(user.Username)}'s balance to {balance:N0} MF$.");
        }

        [Command("setbalance"), RequireRoles(RoleCheckMode.MatchNames, "Minefield Janitor")]
        public async Task SetBalance(CommandContext ctx, string username, int balance)
        {
            var user = await _userService.GetUserByUsernameAsync(username, ctx.Guild.Id);

            if (user == null)
            {
                await ctx.RespondAsync($"Could not find user {username}");
                return;
            }

            if (balance < 0)
            {
                await ctx.RespondAsync("Balance cannot be negative.");
                return;
            }

            user.Currency = balance;

            await _userService.SaveAsync();
            await ctx.RespondAsync($"Set {Formatter.Sanitize(user.Username)}'s balance to {balance:N0} MF$.");
        }

        [Command("setmaxodds"), RequireRoles(RoleCheckMode.MatchNames, "Minefield Janitor")]
        public async Task SetMaxOdds(CommandContext ctx, int max)
        {
            var user = await _userService.GetOrCreateUserAsync(ctx.User.Id, ctx.Guild.Id, ctx.User.Username);

            if (max < 2 || 50 < max)
            {
                await ctx.RespondAsync("Max odds must be between 2 and 50.");
                return;
            }

            user.MaxOdds = max;
            user.CurrentOdds = Math.Min(user.CurrentOdds, user.MaxOdds);

            await _userService.SaveAsync();
            await ctx.RespondAsync($"Set {Formatter.Sanitize(user.Username)}'s max odds to 1 in {user.MaxOdds}.");
        }

        [Command("setmaxodds"), RequireRoles(RoleCheckMode.MatchNames, "Minefield Janitor")]
        public async Task SetMaxOdds(CommandContext ctx, string username, int max)
        {
            var user = await _userService.GetUserByUsernameAsync(username, ctx.Guild.Id);

            if (user == null)
            {
                await ctx.RespondAsync($"Could not find user {username}");
                return;
            }

            if (max < 2 || 50 < max)
            {
                await ctx.RespondAsync("Max odds must be between 2 and 50.");
                return;
            }

            user.MaxOdds = max;
            user.CurrentOdds = Math.Min(user.CurrentOdds, user.MaxOdds);

            await _userService.SaveAsync();
            await ctx.RespondAsync($"Set {Formatter.Sanitize(user.Username)}'s max odds to 1 in {user.MaxOdds}.");
        }

        [Command("setodds"), RequireRoles(RoleCheckMode.MatchNames, "Minefield Janitor")]
        public async Task SetOdds(CommandContext ctx, int odds)
        {
            var user = await _userService.GetOrCreateUserAsync(ctx.User.Id, ctx.Guild.Id, ctx.User.Username);

            if (odds < 2 || user.MaxOdds < odds)
            {
                await ctx.RespondAsync("Odds must be between 2 and max odds.");
                return;
            }

            user.CurrentOdds = odds;

            await _userService.SaveAsync();
            await ctx.RespondAsync($"Set {Formatter.Sanitize(user.Username)}'s odds to 1 in {user.CurrentOdds}.");
        }

        [Command("setodds"), RequireRoles(RoleCheckMode.MatchNames, "Minefield Janitor")]
        public async Task SetOdds(CommandContext ctx, string username, int odds)
        {
            var user = await _userService.GetUserByUsernameAsync(username, ctx.Guild.Id);

            if (user == null)
            {
                await ctx.RespondAsync($"Could not find user {username}");
                return;
            }

            if (odds < 2 || user.MaxOdds < odds)
            {
                await ctx.RespondAsync("Odds must be between 2 and max odds.");
                return;
            }

            user.CurrentOdds = odds;

            await _userService.SaveAsync();
            await ctx.RespondAsync($"Set {Formatter.Sanitize(user.Username)}'s odds to 1 in {user.CurrentOdds}.");
        }

        [Command("status")]
        public async Task Status(CommandContext ctx)
        {
            var user = await _userService.GetOrCreateUserAsync(ctx.User.Id, ctx.Guild.Id, ctx.User.Username);

            await _embedService.SendStatusEmbedAsync(ctx, user);
        }

        [Command("status")]
        public async Task Status(CommandContext ctx, string username)
        {
            var user = await _userService.GetUserByUsernameAsync(username, ctx.Guild.Id);

            if (user == null)
            {
                await ctx.RespondAsync($"Could not find user {username}");
                return;
            }

            await _embedService.SendStatusEmbedAsync(ctx, user);
        }

        [Command("streak")]
        public async Task Streak(CommandContext ctx)
        {
            var user = await _userService.GetOrCreateUserAsync(ctx.User.Id, ctx.Guild.Id, ctx.User.Username);

            await ctx.RespondAsync($"{Formatter.Sanitize(user.Username)} is currently on a streak of {user.CurrentStreak:N0}.");
        }

        [Command("streak")]
        public async Task Streak(CommandContext ctx, string username)
        {
            var user = await _userService.GetUserByUsernameAsync(username, ctx.Guild.Id);

            if (user == null)
            {
                await ctx.RespondAsync($"Could not find user {username}");
                return;
            }

            await ctx.RespondAsync($"{Formatter.Sanitize(user.Username)} is currently on a streak of {user.CurrentStreak:N0}.");
        }

        [Command("users")]
        public async Task Users(CommandContext ctx) => await _embedService.SendUsersEmbedAsync(ctx);
    }
}
