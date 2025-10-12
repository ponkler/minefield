using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Minefield.Entities;
using Minefield.Services;

namespace Minefield.Commands
{
    public class EventCommands : BaseCommandModule
    {
        private readonly CofferService _cofferService;
        private readonly MinefieldService _minefieldService;
        private readonly UserService _userService;

        private readonly EmbedService _embedService;

        public EventCommands(CofferService cofferService, MinefieldService minefieldService, UserService userService, EmbedService embedService)
        {
            _cofferService = cofferService;
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

        [Command("coffer")]
        public async Task Coffer(CommandContext ctx)
        {
            int sum = await _cofferService.GetCofferTicketSaleCountAsync(ctx.Guild.Id);
            int required = await _minefieldService.CalculateRequiredTicketsToOpenCofferAsync(ctx.Guild.Id);

            await ctx.RespondAsync($"There is currently {await _cofferService.GetCofferAmountAsync(ctx.Guild.Id):N0} MF$ in Charon's Coffer. {required} ticket sales are required to open the Coffer. {sum} tickets have been sold so far.");
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

        [Command("tickets")]
        public async Task Tickets(CommandContext ctx)
        {
            var user = await _userService.GetOrCreateUserAsync(ctx.User.Id, ctx.Guild.Id, ctx.User.Username);
            var hasTickets = await _cofferService.CheckIfUserHasTicketsAsync(user);
            var tickets = await _cofferService.GetUserTicketCountAsync(user);

            if (!hasTickets)
            {
                await ctx.RespondAsync($"You don't have any tickets for Charon's Coffer. Your next ticket will cost 20 MF$.");
                return;
            }

            await ctx.RespondAsync($"You have {tickets} ticket(s) for Charon's Coffer. Your next ticket will cost {20 * (int)Math.Pow(2, tickets)} MF$.");
        }

        [Command("tickets")]
        public async Task Tickets(CommandContext ctx, int amount)
        {
            var user = await _userService.GetOrCreateUserAsync(ctx.User.Id, ctx.Guild.Id, ctx.User.Username);

            if ((await _cofferService.GetOrCreateCofferAsync(ctx.Guild.Id)).Opening)
            {
                await ctx.RespondAsync($"The coffer is already opening. You can't buy tickets.");
                return;
            }

            var result = await _minefieldService.BuyCofferTickets(user, amount);

            if (result.Succeeded)
            {
                await ctx.RespondAsync($"You have purchased {amount:N0} tickets for {result.Cost:N0} MF$.");
            }
            else
            {
                await ctx.RespondAsync($"You need {result.Cost:N0} MF$ to buy {amount:N0} tickets. You only have {user.Currency:N0} MF$.");
            }

            if (await _minefieldService.ShouldOpenCoffer(ctx.Guild.Id))
            {
                await _cofferService.ToggleCofferOpening(ctx.Guild.Id);
                await _embedService.SendCofferReadyToOpenEmbedAsync(ctx);
                MinefieldUser winner = await _minefieldService.GetCofferWinner(ctx.Guild.Id);
                await Task.Delay(3000);
                await _embedService.SendCofferPayoutEmbedAsync(ctx, winner);

                winner.Currency += await _cofferService.GetCofferAmountAsync(ctx.Guild.Id);
                winner.LifetimeCurrency += await _cofferService.GetCofferAmountAsync(ctx.Guild.Id);
                await _userService.SaveAsync();

                await _minefieldService.ResetCoffer(ctx.Guild.Id);
            }
        }

        [Command("flip")]
        public async Task Flip(CommandContext ctx) 
        {
            var user = await _userService.GetOrCreateUserAsync(ctx.User.Id, ctx.Guild.Id, ctx.User.Username);

            if (user.Currency < 50)
            {
                await ctx.RespondAsync($"Flips cost 50 MF$. You only have {user.Currency:N0} MF$.");
                return;
            }

            if (user.MessagesSinceCoinFlip < _minefieldService.flipCooldown)
            {
                await ctx.RespondAsync($"You must send {_minefieldService.flipCooldown - (user.MessagesSinceCoinFlip)} more messages before you can Flip again.");
                return;
            }

            user.Currency -= 50;
            await _userService.SaveAsync();

            var result = await _minefieldService.FlipCoin(user);

            switch (result)
            {
                case MinefieldService.FlipResult.Boom:
                    {
                        await ctx.RespondAsync(":boom: Flip exploded! You must send 20 messages before you can flip again.");
                    }
                    break;
                case MinefieldService.FlipResult.Win:
                    {
                        await ctx.RespondAsync(":crown: Flip won! You have earned 100 MF$. You must send 5 messages before you can flip again.");
                    }
                    break;
                case MinefieldService.FlipResult.Loss:
                    {
                        await ctx.RespondAsync(":coffin: Flip lost! You have lost 50 MF$. You must send 5 messages before you can flip again.");
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
