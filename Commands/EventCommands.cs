using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Minefield.Services;

namespace Minefield.Commands
{
    public class EventCommands : BaseCommandModule
    {
        private readonly MinefieldService _minefieldService;
        private readonly UserService _userService;

        private readonly EmbedService _embedService;

        public EventCommands(MinefieldService minefieldService, UserService userService, EmbedService embedService)
        {
            _minefieldService = minefieldService;
            _userService = userService;
            _embedService = embedService;
        }

        [Command("arena")]
        public async Task Arena(CommandContext ctx)
        {
            await ctx.RespondAsync($"You must specify the buy in.");
            return;
        }

        [Command("arena")]
        public async Task Arena(CommandContext ctx, int buyIn)
        {
            var user = await _userService.GetOrCreateUserAsync(ctx.User.Id, ctx.Guild.Id, ctx.User.Username);

            if (_minefieldService.ArenaActive())
            {
                await ctx.RespondAsync($"There is already an active Arena.");
                return;
            }

            if (buyIn <= 0)
            {
                await ctx.RespondAsync($"Arena buy in must be positive.");
                return;
            }

            if (user.Currency < buyIn)
            {
                await ctx.RespondAsync($"You don't have enough MF$ to start this Arena.");
                return;
            }

            await _embedService.SendArenaStartedEmbedAsync(ctx, user, buyIn);

            var arenaCreated = await _minefieldService.SetUpArenaAsync(ctx, buyIn, user);
            if (!arenaCreated)
            {
                await _embedService.SendArenaCancelledEmbedAsync(ctx);
                return;
            }
        }

        [Command("join")]
        public async Task Join(CommandContext ctx)
        {
            var user = await _userService.GetOrCreateUserAsync(ctx.User.Id, ctx.Guild.Id, ctx.User.Username);

            if (!_minefieldService.ArenaActive())
            {
                await ctx.RespondAsync($"There is no Arena to join.");
                return;
            }

            if (_minefieldService.IsInArena(user))
            {
                await ctx.RespondAsync($"You are already in the Arena.");
                return;
            }

            if (!_minefieldService.CanJoinArena(user))
            {
                await ctx.RespondAsync($"You don't have enough MF$ to join this Arena.");
                return;
            }

            await _minefieldService.AddUserToArenaAsync(user);
            await ctx.RespondAsync($":crossed_swords: You have joined the Arena! :crossed_swords:");

        }
    }
}
