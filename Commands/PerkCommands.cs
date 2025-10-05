using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Minefield.Services;

namespace Minefield.Commands
{
    public class PerkCommands : BaseCommandModule
    {
        private readonly MinefieldService _minefieldService;
        private readonly UserService _userService;

        private readonly EmbedService _embedService;

        public PerkCommands(MinefieldService minefieldService, UserService userService, EmbedService embedService) 
        {
            _minefieldService = minefieldService;
            _userService = userService;
            _embedService = embedService;
        }

        [Command("aegis")]
        public async Task Aegis(CommandContext ctx)
        {
            var user = await _userService.GetOrCreateUserAsync(ctx.User.Id, ctx.Guild.Id, ctx.User.Username);

            if (user.AegisCharges > 0)
            {
                await ctx.RespondAsync($"Aegis is already activated. You have {user.AegisCharges} protected messages remaining.");
                return;
            }

            if (user.MessagesSinceAegis < _minefieldService.perkCooldowns["aegis"])
            {
                await ctx.RespondAsync($"You must send {_minefieldService.perkCooldowns["aegis"] - (user.MessagesSinceAegis)} more messages before you can activate Aegis again.");
                return;
            }

            bool activated = await _minefieldService.ActivateAegisAsync(user);
            if (activated)
            {
                await ctx.RespondAsync(":shield: Aegis activated! Your next 5 messages are protected. :shield:");
            }
            else
            {
                await ctx.RespondAsync("You don't have enough MF$ to activate Aegis.");
            }
        }

        [Command("deathpact")]
        public async Task DeathPact(CommandContext ctx)
        {
            await ctx.RespondAsync("You must specify a target.");
            return;
        }

        [Command("deathpact")]
        public async Task DeathPact(CommandContext ctx, string username)
        {
            var user = await _userService.GetOrCreateUserAsync(ctx.User.Id, ctx.Guild.Id, ctx.User.Username);
            var target = await _userService.GetUserByUsernameAsync(username, ctx.Guild.Id);

            if (target == null) 
            {
                await ctx.RespondAsync($"Could not find user {username}");
                return;
            }

            if (user == target)
            {
                await ctx.RespondAsync("You can't target yourself");
                return;
            }

            if (user.DeathPactTarget != null)
            {
                await ctx.RespondAsync("You already have a Death Pact. Use '!enddeathpact' before you activate it again.");
                return;
            }

            if (!target.IsAlive)
            {
                await ctx.RespondAsync("Target user is dead. You can only make Death Pacts with living users.");
                return;
            }

            if (target.DeathPactTarget != null)
            {
                await ctx.RespondAsync("Target user already has a Death Pact.");
                return;
            }

            if (user.Currency < _minefieldService.perkCosts["death_pact"])
            {
                await ctx.RespondAsync("You don't have enough MF$ to activate a Death Pact.");
                return;
            }

            if (target.Currency < _minefieldService.perkCosts["death_pact"])
            {
                await ctx.RespondAsync("Target user doesn't have enough MF$ to activate a Death Pact.");
                return;
            }

            if ((await _userService.GetLinkedUsers(user)).Contains(target))
            {
                await ctx.RespondAsync("You are already linked to that user with another perk.");
                return;
            }

            var deathPactAccepted = await _embedService.SendDeathPactEmbedAsync(ctx, target);
            if (!deathPactAccepted)
            {
                return;
            }

            var deathPactActivated = await _minefieldService.ActivateDeathPactAsync(user, target);
            if (deathPactActivated)
            {
                await ctx.RespondAsync(":scroll: Death Pact activated! You both receive each others earnings. If one of you blows up, the other will blow up with them. On death, you will lose 5 max odds instead of 2. :scroll:");
            }
        }

        [Command("fortune")]
        public async Task Fortune(CommandContext ctx)
        {
            var user = await _userService.GetOrCreateUserAsync(ctx.User.Id, ctx.Guild.Id, ctx.User.Username);

            if (user.FortuneCharges > 0)
            {
                await ctx.RespondAsync($"You already have Fortune activated. You have {user.FortuneCharges} Fortune messages remaining");
                return;
            }

            var fortuneActivated = await _minefieldService.ActivateFortuneAsync(user);
            if (fortuneActivated)
            {
                await ctx.RespondAsync(":coin: Fortune activated! The earnings of your next 5 messages are doubled. :coin:");
            }
            else
            {
                await ctx.RespondAsync("You don't have enough MF$ to activate Fortune.");
            }
        }

        [Command("guardian")]
        public async Task Guardian(CommandContext ctx)
        {
            var user = await _userService.GetOrCreateUserAsync(ctx.User.Id, ctx.Guild.Id, ctx.User.Username);

            if (user.HasGuardian)
            {
                await ctx.RespondAsync("You already have Guardian activated.");
                return;
            }

            if (user.MessagesSinceGuardian < _minefieldService.perkCooldowns["guardian"])
            {
                await ctx.RespondAsync($"You must send {_minefieldService.perkCooldowns["guardian"] - (user.MessagesSinceGuardian)} more messages before you can activate Guardian again.");
                return;
            }

            bool guardianActivated = await _minefieldService.ActivateGuardianAsync(user);
            if (guardianActivated)
            {
                await ctx.RespondAsync(":angel: Guardian activated! The next mine you trigger will be negated. :angel:");
            }
            else
            {
                await ctx.RespondAsync("You don't have enough MF$ to activate Guardian.");
            }
        }

        [Command("lifeline")]
        public async Task Lifeline(CommandContext ctx)
        {
            await ctx.RespondAsync("You must specify a target.");
            return;
        }

        [Command("lifeline")]
        public async Task Lifeline(CommandContext ctx, string username)
        {
            var user = await _userService.GetOrCreateUserAsync(ctx.User.Id, ctx.Guild.Id, ctx.User.Username);
            var target = await _userService.GetUserByUsernameAsync(username, ctx.Guild.Id);

            if (target == null)
            {
                await ctx.RespondAsync("You must specify a target.");
                return;
            }

            if (user == target)
            {
                await ctx.RespondAsync("You can't target yourself");
                return;
            }

            if (user.LifelineTarget != null)
            {
                await ctx.RespondAsync("You already have Lifeline activated. Use '!endlifeline' before you activate it again.");
                return;
            }

            if (target!.IsAlive)
            {
                await ctx.RespondAsync("Target user is still alive. Lifeline can only be used on dead users.");
                return;
            }

            if (target.SacrificeTarget == user)
            {
                await ctx.RespondAsync("You cannot Lifeline a user who Sacrificed themself for you.");
                return;
            }

            if ((await _userService.GetLinkedUsers(user)).Contains(target))
            {
                await ctx.RespondAsync("You are already linked to that user with another perk.");
                return;
            }

            var lifelineActivated = await _minefieldService.ActivateLifelineAsync(user, target);
            if (lifelineActivated)
            {
                var lifelineName = Formatter.Sanitize(target.Username);
                await ctx.RespondAsync($":drop_of_blood: Lifeline activated! You have revived {lifelineName} and you will both receive the earnings of their next 10 messages. :drop_of_blood:");
                await ctx.Channel.SendMessageAsync($"{(await ctx.Guild.GetMemberAsync(target.UserId)).Mention}, you have been revived.");
            }
            else
            {
                await ctx.RespondAsync("You don't have enough MF$ to activate Lifeline.");
            }
        }

        [Command("luck")]
        public async Task Luck(CommandContext ctx, int amount)
        {
            var user = await _userService.GetOrCreateUserAsync(ctx.User.Id, ctx.Guild.Id, ctx.User.Username);

            if (user.CurrentOdds == user.MaxOdds)
            {
                await ctx.RespondAsync("Your odds can't be improved any further.");
                return;
            }

            if (amount <= 0)
            {
                await ctx.RespondAsync("Amount must be greater than 0.");
                return;
            }

            bool luckActivated = await _minefieldService.ActivateLuckAsync(user, amount);
            if (luckActivated)
            {
                await ctx.RespondAsync($":four_leaf_clover: Luck activated! Your odds have been improved to 1 in {user.CurrentOdds}. :four_leaf_clover:");
            }
            else
            {
                await ctx.RespondAsync("You don't have enough MF$ to activate Luck.");
            }
        }

        [Command("restore")]
        public async Task Restore(CommandContext ctx, int amount)
        {
            var user = await _userService.GetOrCreateUserAsync(ctx.User.Id, ctx.Guild.Id, ctx.User.Username);

            if (user.MaxOdds == 50)
            {
                await ctx.RespondAsync("Your max odds can't be improved any further.");
                return;
            }

            if (amount <= 0)
            {
                await ctx.RespondAsync("Amount must be greater than 0.");
                return;
            }

            bool restoreActivated = await _minefieldService.ActivateRestoreAsync(user, amount);
            if (restoreActivated)
            {
                await ctx.RespondAsync($":adhesive_bandage: Restore activated! Your max odds have been improved to 1 in {user.MaxOdds}. :adhesive_bandage:");
            }
            else
            {
                await ctx.RespondAsync("You don't have enough MF$ to activate Restore.");
            }
        }

        [Command("sacrifice")]
        public async Task Sacrifice(CommandContext ctx)
        {
            await ctx.RespondAsync("You must specify a target.");
            return;
        }

        [Command("sacrifice")]
        public async Task Sacrifice(CommandContext ctx, string username)
        {
            var user = await _userService.GetOrCreateUserAsync(ctx.User.Id, ctx.Guild.Id, ctx.User.Username);
            var target = await _userService.GetUserByUsernameAsync(username, ctx.Guild.Id);

            if (target == null)
            {
                await ctx.RespondAsync($"Could not find user {username}");
                return;
            }

            if (user == target)
            {
                await ctx.RespondAsync("You can't target yourself.");
                return;
            }

            if (user.SacrificeTarget != null)
            {
                await ctx.RespondAsync("You already have Sacrifice activated. Use '!endsacrifice' before you activate it again.");
                return;
            }

            if (target!.SacrificeProvider != null)
            {
                await ctx.RespondAsync("Target user already has a Sacrifice bound to them.");
                return;
            }

            if ((await _userService.GetLinkedUsers(user)).Contains(target))
            {
                await ctx.RespondAsync("You are already linked to that user with another perk.");
                return;
            }

            if (!_minefieldService.CanAssignSacrifice(user, target))
            {
                await ctx.RespondAsync("You can't become a sacrifice for this target. It would form a sacrifice loop.");
                return;
            }

            var sacrificeActivated = await _minefieldService.ActivateSacrificeAsync(user, target);
            if (sacrificeActivated)
            {
                await ctx.RespondAsync($":sheep: Sacrifice activated! Next time {Formatter.Sanitize(target.Username)} would be blown up by a mine, you will be blown up instead. :sheep:");
            }
            else
            {
                await ctx.RespondAsync("You don't have enough MF$ to activate Sacrifice.");
            }
        }

        [Command("symbiote")]
        public async Task Symbiote(CommandContext ctx)
        {
            await ctx.RespondAsync("You must specify a target.");
            return;
        }

        [Command("symbiote")]
        public async Task Symbiote(CommandContext ctx, string username)
        {
            var user = await _userService.GetOrCreateUserAsync(ctx.User.Id, ctx.Guild.Id, ctx.User.Username);
            var target = await _userService.GetUserByUsernameAsync(username, ctx.Guild.Id);

            if (user.SymbioteTarget != null)
            {
                await ctx.RespondAsync("You already have Symbiote activated. Use '!endsymbiote' before you activate it again.");
                return;
            }

            if (target == null)
            {
                await ctx.RespondAsync($"Could not find user {username}");
                return;
            }

            if (user == target)
            {
                await ctx.RespondAsync("You can't target yourself.");
                return;
            }

            if (target.SymbioteProvider != null)
            {
                await ctx.RespondAsync("Target user already has a Symbiote bound to them.");
                return;
            }

            if ((await _userService.GetLinkedUsers(user)).Contains(target))
            {
                await ctx.RespondAsync("You are already linked to that user with another perk.");
                return;
            }

            var symbioteActivated = await _minefieldService.ActivateSymbioteAsync(user, target);
            if (symbioteActivated)
            {
                await ctx.RespondAsync($":link: Symbiote activated! You have bound yourself to {Formatter.Sanitize(target.Username)} and you will both receive the earnings of their next 5 messages. :link:");
            }
            else
            {
                await ctx.RespondAsync("You don't have enough MF$ to activate Symbiote.");
            }
        }

        [Command("endlifeline"), RequireRoles(RoleCheckMode.MatchNames, "Minefield Janitor")]
        public async Task EndLifeline(CommandContext ctx)
        {
            var user = await _userService.GetOrCreateUserAsync(ctx.User.Id, ctx.Guild.Id, ctx.User.Username);

            if (user.LifelineTarget == null)
            {
                await ctx.RespondAsync("You don't have a Lifeline activated.");
                return;
            }

            var lifelineName = Formatter.Sanitize(user.LifelineTarget.Username);
            await _minefieldService.RemoveLifelineAsync(user, user.LifelineTarget);

            await ctx.RespondAsync($"You have ended your Lifeline with {lifelineName}.");
        }

        [Command("endlifeline"), RequireRoles(RoleCheckMode.MatchNames, "Minefield Janitor")]
        public async Task EndLifeline(CommandContext ctx, string username)
        {
            var user = await _userService.GetUserByUsernameAsync(username, ctx.Guild.Id);

            if (user == null)
            {
                await ctx.RespondAsync($"Could not find user {username}");
                return;
            }

            if (user.LifelineTarget == null)
            {
                await ctx.RespondAsync("That user doesn't have a Lifeline activated.");
                return;
            }

            var lifelineName = Formatter.Sanitize(user.LifelineTarget.Username);
            await _minefieldService.RemoveLifelineAsync(user, user.LifelineTarget);

            await ctx.RespondAsync($"You have ended {username}'s Lifeline with {lifelineName}.");
        }

        [Command("endsacrifice"), RequireRoles(RoleCheckMode.MatchNames, "Minefield Janitor")]
        public async Task EndSacrifice(CommandContext ctx) 
        {
            var user = await _userService.GetOrCreateUserAsync(ctx.User.Id, ctx.Guild.Id, ctx.User.Username);

            if (user.SacrificeTarget == null)
            {
                await ctx.RespondAsync("You don't have a Sacrifice activated.");
                return;
            }

            var sacrificeName = Formatter.Sanitize(user.SacrificeTarget.Username);
            await _minefieldService.RemoveSacrificeAsync(user, user.SacrificeTarget);

            await ctx.RespondAsync($"You are no longer a Sacrifice for {sacrificeName}.");
        }

        [Command("endsacrifice"), RequireRoles(RoleCheckMode.MatchNames, "Minefield Janitor")]
        public async Task EndSacrifice(CommandContext ctx, string username)
        {
            var user = await _userService.GetUserByUsernameAsync(username, ctx.Guild.Id);

            if (user == null)
            {
                await ctx.RespondAsync($"Could not find user {username}");
                return;
            }

            if (user.SacrificeTarget == null)
            {
                await ctx.RespondAsync("That user doesn't have a Sacrifice activated.");
                return;
            }

            var sacrificeName = Formatter.Sanitize(user.SacrificeTarget.Username);
            await _minefieldService.RemoveSymbioteAsync(user, user.SacrificeTarget);

            await ctx.RespondAsync($"{username} is no longer a Sacrifice for {sacrificeName}.");
        }

        [Command("endsymbiote"), RequireRoles(RoleCheckMode.MatchNames, "Minefield Janitor")]
        public async Task EndSymbiote(CommandContext ctx)
        {
            var user = await _userService.GetOrCreateUserAsync(ctx.User.Id, ctx.Guild.Id, ctx.User.Username);

            if (user.SymbioteTarget == null)
            {
                await ctx.RespondAsync("You don't have a Sacrifice activated.");
                return;
            }

            var symbioteName = Formatter.Sanitize(user.SymbioteTarget.Username);
            await _minefieldService.RemoveSymbioteAsync(user, user.SymbioteTarget);

            await ctx.RespondAsync($"You have ended your Symbiote with {symbioteName}.");
        }

        [Command("endsymbiote"), RequireRoles(RoleCheckMode.MatchNames, "Minefield Janitor")]
        public async Task EndSymbiote(CommandContext ctx, string username)
        {
            var user = await _userService.GetUserByUsernameAsync(username, ctx.Guild.Id);

            if (user == null)
            {
                await ctx.RespondAsync($"Could not find user {username}");
                return;
            }

            if (user.SymbioteTarget == null)
            {
                await ctx.RespondAsync("That user doesn't have a Symbiote activated.");
                return;
            }

            var symbioteName = Formatter.Sanitize(user.SymbioteTarget.Username);
            await _minefieldService.RemoveSymbioteAsync(user, user.SymbioteTarget);

            await ctx.RespondAsync($"You have ended {username}'s Symbiote with {symbioteName}.");
        }
    }
}
