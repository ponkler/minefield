using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Minefield.Entities;

namespace Minefield.Services
{
    public class EmbedService
    {
        private readonly CofferService _cofferService;
        private readonly MinefieldService _minefieldService;
        private readonly UserService _userService;

        public EmbedService(CofferService cofferService, MinefieldService minefieldService, UserService userService) 
        {
            _cofferService = cofferService;
            _minefieldService = minefieldService;
            _userService = userService;
        }

        public async Task SendArenaStartedEmbedAsync(CommandContext ctx, MinefieldUser host, int buyIn)
        {
            var hostName = Formatter.Sanitize(host.Username);
            var str = $"{hostName} has started an Arena which will begin in 60 seconds. Type \"!join\" to join. The buy in is {buyIn:N0} MF$.";

            var arenaStartedEmbed = new DiscordEmbedBuilder()
                .WithTitle(":rotating_light: Arena Started :rotating_light:")
                .WithColor(DiscordColor.IndianRed)
                .AddField("__Details__", str, false);

            await ctx.Channel.SendMessageAsync(content: $"{ctx.Guild.Roles.Select(r => r.Value).Where(r => r.Name == "Arena").First().Mention}", embed: arenaStartedEmbed);
        }

        public async Task SendArenaCancelledEmbedAsync(CommandContext ctx)
        {
            var arenaCancelledEmbed = new DiscordEmbedBuilder()
                .WithTitle(":x: Arena Cancelled :x:")
                .WithColor(DiscordColor.IndianRed)
                .AddField("__Details__", $"Nobody joined the arena. The buy in has been refunded.", false);

            await ctx.RespondAsync(embed: arenaCancelledEmbed);
        }

        public async Task SendArenaRoundEmbedAsync(CommandContext ctx, Dictionary<MinefieldUser, int> participantRolls, int round)
        {
            List<string> rollStrings = new List<string>();

            foreach (var entry in participantRolls)
            {
                string entryName = Formatter.Sanitize(entry.Key.Username);
                string str = $"\t• {entryName} - {entry.Value}/5 {(entry.Value == 5 ? ":boom:" : ":ok:")}";
                rollStrings.Add(str);
            }

            var arenaRoundEmbed = new DiscordEmbedBuilder()
                .WithTitle($":crossed_swords: Arena Round {round} :crossed_swords:")
                .WithColor(DiscordColor.IndianRed)
                .AddField("__Rolls__", string.Join("\n", rollStrings), false);

            await ctx.Channel.SendMessageAsync(embed: arenaRoundEmbed);
        }

        public async Task SendArenaParticipantsEmbedAsync(CommandContext ctx, List<MinefieldUser> participants, int payout)
        {
            List<string> users = new List<string>();

            foreach (var participant in participants)
            {
                string participantName = Formatter.Sanitize(participant.Username);
                string str = $"\t• {participantName}";
                users.Add(str);
            }

            var arenaResolveEmbed = new DiscordEmbedBuilder()
                .WithTitle(":dagger: Arena Participants :dagger:")
                .WithColor(DiscordColor.IndianRed)
                .AddField("__List__", string.Join("\n", users), false)
                .AddField("__Payout__", $"\t• {payout:N0} MF$");
            
            await ctx.Channel.SendMessageAsync(embed: arenaResolveEmbed);
        }

        public async Task SendArenaResolveEmbedAsync(CommandContext ctx, List<MinefieldUser> winners, int payout)
        {
            int split = (int)(payout / winners.Count);

            List<string> payoutStrings = new List<string>();

            foreach (var winner in winners)
            {
                var winnerName = Formatter.Sanitize(winner.Username);

                string str = $"\t• {winnerName} - {split:N0} MF$";
                payoutStrings.Add(str);
            }

            var arenaResolveEmbed = new DiscordEmbedBuilder()
                .WithTitle(":trophy: Arena Winners :trophy:")
                .WithColor(DiscordColor.Gold)
                .AddField("__Winners__", string.Join("\n", payoutStrings), false);

            await ctx.Channel.SendMessageAsync(embed: arenaResolveEmbed);
        }

        public async Task SendCofferReadyToOpenEmbedAsync(CommandContext ctx)
        {
            List<string> entryStrings = new List<string>();
            DiscordRole cofferRole = ctx.Guild.Roles.Select(r => r.Value).Where(r => r.Name == "Coffer").First();

            foreach (var entry in await _cofferService.GetUserTicketsAsync(ctx.Guild.Id))
            {
                string str = $"\t• {Formatter.Sanitize(entry.User.Username)}, Tickets: {entry.Amount}";
                entryStrings.Add(str);
            }

            var cofferReadyEmbed = new DiscordEmbedBuilder()
                .WithTitle(":urn: Coffer Opening :urn:")
                .WithColor(DiscordColor.Aquamarine)
                .AddField("__Entrants__", string.Join("\n", entryStrings));

            await ctx.Channel.SendMessageAsync(content: cofferRole.Mention, embed: cofferReadyEmbed);
        }

        public async Task SendCofferPayoutEmbedAsync(CommandContext ctx, MinefieldUser winner)
        {
            var cofferWinnerEmbed = new DiscordEmbedBuilder()
                .WithTitle(":gem: Coffer Winner :gem:")
                .WithColor(DiscordColor.Aquamarine)
                .AddField("__Winner__", $"{Formatter.Sanitize(winner.Username)} has opened Charon's Coffer and won {await _cofferService.GetCofferAmountAsync(ctx.Guild.Id):N0} MF$.");

            await ctx.RespondAsync(embed: cofferWinnerEmbed);
        }

        public async Task SendCooldownEmbedAsync(CommandContext ctx, MinefieldUser user)
        {
            var cooldownStrings = new List<string>();

            if (user.AegisCharges > 0)
            {
                cooldownStrings.Add($":shield: **Aegis:** Active ({user.AegisCharges} messages remaining)");
            }
            else
            {
                if (user.MessagesSinceAegis >= _minefieldService.perkCooldowns["aegis"])
                {
                    cooldownStrings.Add(":shield: **Aegis:** Available");
                }
                else
                {
                    cooldownStrings.Add($":shield: **Aegis:** On Cooldown ({_minefieldService.perkCooldowns["aegis"] - user.MessagesSinceAegis} messages remaining)");
                }
            }

            if (user.HasGuardian)
            {
                cooldownStrings.Add($":angel: **Guardian:** Active");
            }
            else
            {
                if (user.MessagesSinceGuardian >= _minefieldService.perkCooldowns["guardian"])
                {
                    cooldownStrings.Add(":angel: **Guardian:** Available");
                }
                else
                {
                    cooldownStrings.Add($":angel: **Guardian:** On Cooldown ({_minefieldService.perkCooldowns["guardian"] - user.MessagesSinceGuardian} messages remaining)");
                }
            }

            var cooldownEmbed = new DiscordEmbedBuilder()
                .WithTitle(":arrows_counterclockwise: Perk Cooldowns :arrows_counterclockwise:")
                .WithColor(DiscordColor.PhthaloGreen)
                .AddField("__Perks__", string.Join("\n", cooldownStrings));

            await ctx.RespondAsync(embed: cooldownEmbed);
        }

        public async Task<bool> SendDeathPactEmbedAsync(CommandContext ctx, MinefieldUser targetUser)
        {
            var deathPactEmbed = new DiscordEmbedBuilder()
                .WithTitle(":scroll: Death Pact :scroll:")
                .WithColor(DiscordColor.Black)
                .AddField("__Offer__", $"{Formatter.Sanitize(targetUser.Username)}, {Formatter.Sanitize(ctx.User.Username)} has offered you a Death Pact. If you agree, you and {Formatter.Sanitize(ctx.User.Username)} " +
                $"will share message earnings, but if either of you trigger a mine, you will both blow up and lose 5 max odds instead of 2.", false)
                .AddField("__Options__", $"\t• React :pen_fountain: to accept the Death Pact offer.\n" +
                $"\t• React :x: to decline the Death Pact offer.");

            var member = await ctx.Client.GetUserAsync(targetUser.UserId);
            var penEmoji = DiscordEmoji.FromName(ctx.Client, ":pen_fountain:");
            var cancelEmoji = DiscordEmoji.FromName(ctx.Client, ":x:");
            var message = await ctx.Client.SendMessageAsync(ctx.Channel, embed: deathPactEmbed);

            await message.CreateReactionAsync(penEmoji);
            await message.CreateReactionAsync(cancelEmoji);

            var interactivity = ctx.Client.GetInteractivity();

            var reactionResult = await interactivity.WaitForReactionAsync(
                x => x.Message == message
                     && x.User == member
                     && (x.Emoji == penEmoji || x.Emoji == cancelEmoji)
            );

            if (reactionResult.TimedOut)
            {
                await ctx.RespondAsync($"{Formatter.Sanitize(targetUser.Username)} did not respond to your Death Pact offer.");
                return false;
            }
            else if (reactionResult.Result.Emoji == penEmoji)
            {
                return true;
            }
            else if (reactionResult.Result.Emoji == cancelEmoji)
            {
                await ctx.RespondAsync($"{Formatter.Sanitize(targetUser.Username)} declined your Death Pact offer.");
                return false;
            }

            return false;
        }

        public async Task SendInfoEmbedAsync(CommandContext ctx)
        {
            var infoEmbed = new DiscordEmbedBuilder()
                .WithTitle(":clipboard: Minefield Information :clipboard:")
                .WithColor(DiscordColor.White)
                .AddField("__General__",
                    $"\t• The minefield is a channel where your messages have a chance to explode and kill you, but you can buy useful perks to prevent this.\n" +
                    $"\t• Exploding in the minefield removes your permissions to view and send messages in the channel. It also lowers your maximum odds by 2 and resets your streak and perks.\n" +
                    $"\t• Everybody starts with a 1 in 50 chance that their message will trigger a mine. This reduces with each message sent, to 1 in 49, 1 in 48, etc. The worst possible odds are 1 in 2.\n" +
                    $"\t• Every message you send increases your streak by 1, and you earn Minefield Dollars (MF$) equal to your current streak when you send a message.\n" +
                    $"\t• Sending a command does not roll for a mine or increase your streak.\n" +
                    $"\t• You can buy perks to increase your earnings, or try to survive longer. To see these, use the \"!perks\" command.\n"
                )
                .AddField("__Tips__",
                    $"\t• Aegis and Guardian cooldowns don't begin until the perk has been used up.\n" +
                    $"\t• You may only have one link to a user. So, if you have a Sacrifice linked to a user, you cannot have a Symbiote linked to the same user.\n" +
                    $"\t• The best possible odds are 1 in 50. If you attempt to use Luck or Restore to go beyond this limit, the amount will automatically be capped.\n" +
                    $"\t• In a Sacrifice chain, Guardian will only be used on the final user."
                )
                .AddField("__Tips Continued__",
                    $"\t• If you have Guardian activated, and your Death Pact partner triggers a mine, your Guardian will not defend you. If your partner has Guardian, and they trigger a mine, their Guardian will protect you both.\n" +
                    $"\t• The above is also true for Sacrifice. If your Death Pact partner triggers a mine, but you have a Sacrifice linked to you, your Sacrifice will not save you, but your partners will save you both.\n"
                );

            await ctx.RespondAsync(embed: infoEmbed);
        }

        public async Task SendStatusEmbedAsync(CommandContext ctx, MinefieldUser targetUser)
        {
            string perkString =
                $"{(targetUser.AegisCharges > 0 ? ":shield:" : "")}" +
                $"{(targetUser.DeathPactTarget != null ? ":scroll:" : "")}" +
                $"{(targetUser.HasGuardian ? ":angel:" : "")}" +
                $"{(targetUser.LifelineTarget != null ? ":drop_of_blood:" : "")}" +
                $"{(targetUser.FortuneCharges > 0 ? ":coin:" : "")}" +
                $"{(targetUser.SacrificeTarget != null ? ":sheep:" : "")}" +
                $"{(targetUser.SymbioteTarget != null ? ":link:" : "")}";

            var embed = new DiscordEmbedBuilder()
                .WithTitle($"{(targetUser.IsAlive ? targetUser.IsImmune ? ":moyai:" : ":heart:" : ":headstone:")} {Formatter.Sanitize(targetUser.Username)} {perkString}")
                .WithColor(targetUser.IsAlive ? targetUser.IsImmune ? DiscordColor.Gray : DiscordColor.Green : DiscordColor.Red)
                .AddField("__Stats__",
                    $"\t• **Odds:** 1 in {targetUser.CurrentOdds}\n" +
                    $"\t• **Max Odds:** 1 in {targetUser.MaxOdds}\n" +
                    $"\t• **MF$:** {targetUser.Currency:N0}\n" +
                    $"\t• **Streak:** {targetUser.CurrentStreak}\n" +
                    $"\t• **Total Messages:** {targetUser.TotalMessages}\n" +
                    $"\t• **Lifetime MF$:** {targetUser.LifetimeCurrency:N0}");

            var perkDetails = new List<string>();
            if (targetUser.AegisCharges > 0) perkDetails.Add($"\t• :shield: **Aegis Messages Remaining:** {targetUser.AegisCharges}");
            if (targetUser.DeathPactTarget != null) perkDetails.Add($"\t• :scroll: **Death Pact:** {Formatter.Sanitize(targetUser.DeathPactTarget.Username)}");
            if (targetUser.HasGuardian) perkDetails.Add($"\t• :angel: **Guardian:** Active");
            if (targetUser.LifelineTarget != null) perkDetails.Add($"\t• :drop_of_blood: **Lifeline Target:** {Formatter.Sanitize(targetUser.LifelineTarget.Username)} ({targetUser.LifelineCharges} messages remaining)");
            if (targetUser.FortuneCharges > 0) perkDetails.Add($"\t• :coin: **Fortune Messages Remaining:** {targetUser.FortuneCharges}");
            if (targetUser.SacrificeTarget != null) perkDetails.Add($"\t• :sheep: **Sacrifice Target:** {Formatter.Sanitize(targetUser.SacrificeTarget.Username)}");
            if (targetUser.SymbioteTarget != null) perkDetails.Add($"\t• :link: **Symbiote Target:** {Formatter.Sanitize(targetUser.SymbioteTarget.Username)} ({targetUser.SymbioteCharges} messages remaining)");


            var providerDetails = new List<string>();
            if (targetUser.LifelineProvider != null) providerDetails.Add($"\t• :drop_of_blood: **Lifeline Provider:** {Formatter.Sanitize(targetUser.LifelineProvider.Username)} ({targetUser.LifelineProvider.LifelineCharges} messages remaining)");
            if (targetUser.SacrificeProvider != null) providerDetails.Add($"\t• :sheep: **Sacrifice Provider:** {Formatter.Sanitize(targetUser.SacrificeProvider.Username)}");
            if (targetUser.SymbioteProvider != null) providerDetails.Add($"\t• :link: **Symbiote Provider:** {Formatter.Sanitize(targetUser.SymbioteProvider.Username)} ({targetUser.SymbioteProvider.SymbioteCharges} messages remaining)");

            if (perkDetails.Count > 0)
                embed.AddField("__Perks__", string.Join("\n", perkDetails));

            if (providerDetails.Count > 0)
                embed.AddField("__Providers__", string.Join("\n", providerDetails));

            await ctx.RespondAsync(embed: embed);
        }

        public async Task SendHelpEmbedAsync(CommandContext ctx)
        {
            var helpEmbed = new DiscordEmbedBuilder()
                .WithTitle(":bulb: Minefield Commands :bulb:")
                .WithColor(DiscordColor.Rose)
                .AddField("__Self Perks__",
                    $"\t• **!aegis** — Your next 5 messages are protected from mines. (20 message cooldown after depletion)\n" +
                    $"\t• **!fortune** — Double the earnings of your next 5 messages.\n" +
                    $"\t• **!guardian** — Negate the effects of the next mine you trigger. In Sacrifice chains, Guardian will only trigger for the final user. (15 message cooldown after depletion)\n" +
                    $"\t• **!luck** — Improve your current odds by a chosen amount. (up to your maximum)\n" +
                    $"\t• **!restore** — Increase your maximum odds, and current odds, by a chosen amount. (up to a maximum of 50)\n"
                )
                .AddField("__Linked Perks__",
                    $"\t• **!deathpact <username>** — You and the target share message earnings. When you or the target trigger a mine, you both blow up and lose 5 max odds.\n" +
                    $"\t• **!lifeline <username>** — Revive a dead user, resetting their odds and streak. You both receive the earnings of their next 10 messages. Cannot be used on a target who has Sacrificed themself for you.\n" +
                    $"\t• **!sacrifice <username>** — The next time your target would be blown up by a mine, you are blown up in their place. You receive MF$ equal to 20% of the target's current MF$. Sacrifice can chain, however, it cannot form a loop.\n" +
                    $"\t• **!symbiote <username>** — Bind yourself to a living user. You both receive the earnings of their next 5 messages.\n"
                )
                .AddField("__Events__",
                    $"\t• **!arena <amount>** — Starts an arena with the given amount as the buy in.\n" +
                    $"\t• **!join** — Joins an active arena.\n" +
                    $"\t• **!coffer** — Show info about Charon's Coffer.\n" +
                    $"\t• **!tickets** — Show info about Coffer tickets.\n" +
                    $"\t• **!tickets <amount>** — Purchase a number of Coffer tickets."
                )
                .AddField("__Utility (Part 1)__",
                    $"\t• **!balance [username]** — Show a user's current MF$.\n" +
                    $"\t• **!cooldowns [username]** — Shows the number of messages you must send before you can activate certain perks again.\n" +
                    $"\t• **!deadusers / !dead** — Show a list of the usernames of all dead users.\n" +
                    $"\t• **!help** — Show this help message.\n" +
                    $"\t• **!info** — Show general information and tips about the minefield.\n" +
                    $"\t• **!maxodds [username]** — Shows a user's max odds.\n" +
                    $"\t• **!odds [username]** — Show a user's current odds.\n" +
                    $"\t• **!perks** — Show all perk prices and descriptions.\n" +
                    $"\t• **!role <role name>** — Grants or revokes a Minefield role.\n" +
                    $"\t• **!roles** — Show all valid Minefield role names."
                )
                .AddField("__Utility (Part 2)__",
                    $"\t• **!status [username]** — View a user's active perks, current streak, odds, and MF$.\n" +
                    $"\t• **!streak [username]** — Show a user's current streak.\n" +
                    $"\t• **!users** — Show a list of the usernames of all users in the server."
                )
                .AddField("__Janitor Commands__",
                    $"\t• **!endlifeline [username]** — Unbinds a user from their Lifeline target.\n" +
                    $"\t• **!endsacrifice [username]** — Unbinds a user from their Sacrifice target.\n" +
                    $"\t• **!endsymbiote [username]** — Unbinds a user from their Symbiote target.\n" +
                    $"\t• **!immune** — Toggles Minefield immunity.\n" +
                    $"\t• **!setbalance [username] <amount>** — Sets a user's balance to the given amount.\n" +
                    $"\t• **!setmaxodds [username] <amount>** — Sets a user's max odds to the given amount.\n" +
                    $"\t• **!setodds [username] <amount>** — Sets a user's odds to the given amount.\n" +
                    $"\t• **!reset [username]** — Wipes a user's progress. (Does not affect permissions. If target user is dead, use \"!revive\" first.)\n" +
                    $"\t• **!revive [username]** — Revives a dead user."
                );

            await ctx.RespondAsync(embed: helpEmbed);
        }

        public async Task SendPerkEmbedAsync(CommandContext ctx)
        {
            var perkEmbed = new DiscordEmbedBuilder()
                .WithTitle(":star: Minefield Perks :star:")
                .WithColor(DiscordColor.Azure)
                .AddField($":shield: Aegis ({_minefieldService.perkCosts["aegis"]} MF$)", "Your next 5 messages are protected from mines. (20 message cooldown after depletion)", false)
                .AddField($":scroll: Death Pact ({_minefieldService.perkCosts["death_pact"]} MF$ from each user)", "You and the target share message earnings. When you or the target trigger a mine, you both blow up and lose 5 max odds.", false)
                .AddField($":coin: Fortune ({_minefieldService.perkCosts["fortune"]} MF$)", "Double the earnings of your next 5 messages.", false)
                .AddField($":angel: Guardian ({_minefieldService.perkCosts["guardian"]} MF$)", "Negate the effects of the next mine you trigger. In Sacrifice chains, Guardian will only trigger for the final user. (15 message cooldown after depletion)", false)
                .AddField($":drop_of_blood: Lifeline ({_minefieldService.perkCosts["lifeline"]} MF$)", "Revive a dead user, resetting their odds and streak. You both receive the earnings of their next 10 messages. Cannot be used on a target that has Sacrificed themself for you.", false)
                .AddField($":four_leaf_clover: Luck ({_minefieldService.perkCosts["luck"]} MF$ per point)", "Improve your current odds by a chosen amount. (up to your maximum)", false)
                .AddField($":adhesive_bandage: Restore ({_minefieldService.perkCosts["restore"]} MF$ per point)", "Increase your maximum odds, and current odds, by a chosen amount. (up to a maximum of 50)", false)
                .AddField($":sheep: Sacrifice ({_minefieldService.perkCosts["sacrifice"]} MF$)", "The next time your target would be blown up by a mine, you are blown up in their place. You receive MF$ equal to 20% of the target's current MF$. Sacrifice can chain, however, it cannot form a loop.", false)
                .AddField($":link: Symbiote ({_minefieldService.perkCosts["symbiote"]} MF$)", "Bind yourself to a living user. You both receive the earnings of their next 5 messages.", false);

            await ctx.RespondAsync(embed: perkEmbed);
        }

        public async Task SendLeaderboardEmbedAsync(CommandContext ctx, List<LeaderboardEntry> leaderboardEntries)
        {
            List<string> entryStrings = new List<string>();

            List<string> prefixes = new List<string>
            {
                ":first_place:",
                ":second_place:",
                ":third_place:",
                ":four:",
                ":five:",
                ":six:",
                ":seven:",
                ":eight:",
                ":nine:",
                ":keycap_ten:"
            };

            var index = 0;

            foreach (var entry in leaderboardEntries)
            {
                string newEntry = $"{prefixes[index]} {entry.Name} - {entry.Currency:N0} MF$";
                entryStrings.Add(newEntry);
                index++;
            }

            var leaderboardEmbed = new DiscordEmbedBuilder()
                .WithTitle(":trophy: Minefield Leaderboard :trophy:")
                .WithColor(DiscordColor.Gold)
                .AddField("__Top 10__", string.Join("\n", entryStrings), false);

            await ctx.RespondAsync(embed: leaderboardEmbed);
        }

        public async Task SendUsersEmbedAsync(CommandContext ctx)
        {
            var usernames = await _userService.GetAllUsernamesAsync(ctx.Guild.Id);

            var userEmbed = new DiscordEmbedBuilder()
                .WithTitle(":person_standing: Minefield Users :person_standing:")
                .WithColor(DiscordColor.Azure)
                .AddField("__List__", Formatter.Sanitize(string.Join("\n", usernames)), false);

            await ctx.RespondAsync(embed: userEmbed);
        }

        public async Task SendDeadUsersEmbedAsync(CommandContext ctx)
        {
            var usernames = await _userService.GetAllDeadUsernamesAsync(ctx.Guild.Id);

            var deadUserEmbed = new DiscordEmbedBuilder()
            .WithTitle(":skull: Dead Users :skull:")
            .WithColor(DiscordColor.Gray)
            .AddField("__Graveyard__", usernames.Count == 0 ? "None" : Formatter.Sanitize(string.Join("\n", usernames)), false);

            await ctx.RespondAsync(embed: deadUserEmbed);
        }
    }

    public class LeaderboardEntry
    {
        public string Name { get; set; } = string.Empty;
        public int Currency { get; set; }
    }
}
