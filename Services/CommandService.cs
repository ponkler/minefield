using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Minefield.Entities;

namespace Minefield.Services
{
    public class CommandService
    {
        private readonly UserService _userService;
        private readonly MinefieldService _minefieldService;

        public CommandService(UserService userService, MinefieldService minefieldService)
        {
            _userService = userService;
            _minefieldService = minefieldService;
        }

        public async Task HandleCommandAsync(DiscordClient client, MessageCreateEventArgs e)
        {
            var parts = e.Message.Content.Trim().Split(' ', 2);
            var command = parts[0].ToLower();

            var user = await _userService.GetOrCreateUserAsync(e.Author.Id, e.Guild.Id);

            ulong targetId = e.Author.Id;

            if (parts.Length > 1)
            {
                var arg = parts[1].Trim();

                var member = e.Guild.Members.Values.FirstOrDefault(
                    m => string.Equals(m.Username, arg, StringComparison.OrdinalIgnoreCase)
                );

                if (member != null)
                {
                    if (member.IsBot)
                    {
                        await e.Message.RespondAsync($"Invalid user.");
                        return;
                    }
                    targetId = member.Id;
                }
                else
                {
                    await e.Message.RespondAsync($"Could not find user `{arg}`.");
                    return;
                }
            }

            var targetUser = await _userService.GetOrCreateUserAsync(targetId, e.Guild.Id);

            switch (command)
            {
                case "!leaderboard":
                    var leaderboard = await _userService.GetLeaderboardAsync(user.ServerId);
                    List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
                    foreach (var entry in leaderboard)
                    {
                        LeaderboardEntry newEntry = new LeaderboardEntry
                        {
                            Name = (await client.GetUserAsync(entry.UserId)).Username,
                            Currency = entry.Currency
                        };

                        entries.Add(newEntry);
                    }
                    await SendLeaderboardEmbedAsync(e, entries);
                    break;
                case "!balance":
                    {
                        var name = (await client.GetUserAsync(targetUser.UserId)).Username;
                        await e.Message.RespondAsync($"{name} has {targetUser.Currency:N0} MF$.");
                        break;
                    }
                    
                case "!odds":
                    {
                        var name = (await client.GetUserAsync(targetUser.UserId)).Username;
                        await e.Message.RespondAsync($"{name}'s odds are 1 in {targetUser.CurrentOdds}.");
                        break;
                    }
                case "!streak":
                    {
                        var name = (await client.GetUserAsync(targetUser.UserId)).Username;
                        await e.Message.RespondAsync($"{name} is currently on a streak of {targetUser.CurrentStreak:N0}.");
                        break;
                    }
                case "!messages":
                    {
                        var name = (await client.GetUserAsync(targetUser.UserId)).Username;
                        await e.Message.RespondAsync($"{name} has sent {targetUser.TotalMessages:N0} messages.");
                        break;
                    }
                case "!perks":
                    {
                        await SendPerkEmbedAsync(e);
                        break;
                    }
                case "!status":
                    await SendStatusEmbedAsync(client, e, targetUser ?? user);
                    break;
                case "!help":
                    await SendHelpEmbedAsync(e);
                    break;
                case "!endlifeline":
                    {
                        if (user.LifelineTarget == null)
                        {
                            await e.Message.RespondAsync("You don't have a Lifeline activated.");
                            return;
                        }

                        var lifelineName = (await client.GetUserAsync(user.LifelineTarget.UserId)).Username;
                        await _minefieldService.RemoveLifelineAsync(user);

                        await e.Message.RespondAsync($"You have ended your Lifeline for {lifelineName}");
                        break;
                    }
                case "!endsacrifice":
                    {
                        if (user.SacrificeTarget == null)
                        {
                            await e.Message.RespondAsync("You don't have a Sacrifice activated.");
                            return;
                        }

                        var sacrificeName = (await client.GetUserAsync(user.SacrificeTarget.UserId)).Username;
                        await _minefieldService.RemoveSacrificeAsync(user);

                        await e.Message.RespondAsync($"You have ended your Sacrifice for {sacrificeName}");
                        break;
                    }
                case "!endsymbiote":
                    {
                        if (user.SymbioteTarget == null)
                        {
                            await e.Message.RespondAsync("You don't have a Symbiote activated.");
                            return;
                        }

                        var symbioteName = (await client.GetUserAsync(user.SymbioteTarget.UserId)).Username;
                        await _minefieldService.RemoveSymbioteAsync(user);

                        await e.Message.RespondAsync($"You have ended your Symbiote for {symbioteName}");
                        break;
                    }
                // AEGIS
                case "!aegis":
                    if (user.AegisCharges > 0)
                    {
                        await e.Message.RespondAsync($"Aegis is already activated. You have {user.AegisCharges} protected messages remaining.");
                        return;
                    }

                    // 20 message cooldown after activating aegis
                    if (user.MessagesSinceAegis <= 20)
                    {
                        await e.Message.RespondAsync($"You must send {20 - user.MessagesSinceAegis} more messages before you can activate Aegis again.");
                        return;
                    }

                    bool aegisActivated = await _minefieldService.ActivateAegisAsync(user);
                    if (aegisActivated)
                    {
                        await e.Message.RespondAsync(":shield: Aegis activated! Your next 5 messages are protected. :shield:");
                    }
                    else
                    {
                        await e.Message.RespondAsync("You don't have enough MF$ to activate Aegis.");
                    }

                    break;
                // FORTUNE
                case "!fortune":
                    // set to max odds
                    if (user.CurrentOdds == 50) 
                    {
                        await e.Message.RespondAsync("Your odds can't be improved any further.");
                        return;
                    }

                    bool fortuneActivated = await _minefieldService.ActivateFortuneAsync(user);
                    if (fortuneActivated)
                    {
                        await e.Message.RespondAsync(":chart_with_upwards_trend: Fortune activated! Your odds have been improved. :chart_with_upwards_trend:");
                    }
                    else
                    {
                        await e.Message.RespondAsync("You don't have enough MF$ to activate Fortune.");
                    }

                    break;
                // GUARDIAN
                case "!guardian":
                    {
                        if (user.HasGuardian)
                        {
                            await e.Message.RespondAsync("You already have Guardian activated.");
                            return;
                        }

                        // 30 message cooldown after activating guardian
                        if (user.MessagesSinceGuardian <= 30)
                        {
                            await e.Message.RespondAsync($"You must send {30 - user.MessagesSinceGuardian} more messages before you can activate Guardian again.");
                            return;
                        }

                        bool guardianActivated = await _minefieldService.ActivateGuardianAsync(user);
                        if (guardianActivated)
                        {
                            await e.Message.RespondAsync(":angel: Guardian activated! The next mine you trigger will be negated. :angel:");
                        }
                        else
                        {
                            await e.Message.RespondAsync("You don't have enough MF$ to activate Guardian.");
                        }
                        break;
                    }

                // LIFELINE
                case "!lifeline":
                    {
                        if (user == targetUser)
                        {
                            await e.Message.RespondAsync("You can't target yourself");
                            return;
                        }

                        if (user.LifelineTarget != null)
                        {
                            await e.Message.RespondAsync("You already have Lifeline activated. Use '!endlifeline' before you activate it again.");
                            return;
                        }

                        if (targetUser.IsAlive)
                        {
                            await e.Message.RespondAsync("Target user is still alive. Lifeline can only be used on dead users.");
                            return;
                        }

                        if (targetUser.LifelineProvider != null)
                        {
                            await e.Message.RespondAsync("Target user already has a Lifeline bound to them.");
                            return;
                        }

                        var lifelineActivated = await _minefieldService.ActivateLifelineAsync(user, targetUser);
                        if (lifelineActivated)
                        {
                            var lifelineName = (await client.GetUserAsync(user.LifelineTarget!.UserId)).Username;
                            await e.Message.RespondAsync($":drop_of_blood: Lifeline activated! You have revived {lifelineName} and you will both receive the earnings of their next 10 messages. :drop_of_blood:");
                        }
                        else
                        {
                            await e.Message.RespondAsync("You don't have enough MF$ to activate Lifeline.");
                        }
                        break;
                    }
                // LUCK
                case "!luck":
                    if (user.LuckCharges > 0)
                    {
                        await e.Message.RespondAsync($"You already have Luck activated. You have {user.LuckCharges} lucky messages remaining");
                        return;
                    }

                    var luckActivated = await _minefieldService.ActivateLuckAsync(user);
                    if (luckActivated)
                    {
                        await e.Message.RespondAsync(":four_leaf_clover: Luck activated! The earnings of your next 5 messages are doubled. :four_leaf_clover:");
                    }
                    else
                    {
                        await e.Message.RespondAsync("You don't have enough MF$ to activate Luck.");
                    }
                    break;
                case "!reset":
                    var server = await client.GetGuildAsync(user.ServerId);
                    var member = await server.GetMemberAsync(user.UserId);

                    if (member.Roles.Any(r => r.Name == "Minefield Janitor"))
                    {
                        var id = targetUser.UserId;
                        var serverId = targetUser.ServerId;

                        targetUser = await _userService.ResetUserAsync(id, serverId);

                        var targetName = (await client.GetUserAsync(id)).Username;

                        await e.Message.RespondAsync($"{targetName}'s progress has been reset.");
                        await _userService.SaveAsync();
                    }
                    break;
                // SACRIFICE
                case "!sacrifice":
                    {
                        if (user == targetUser)
                        {
                            await e.Message.RespondAsync("You can't target yourself");
                            return;
                        }

                        if (user.SacrificeTarget != null)
                        {
                            await e.Message.RespondAsync("You already have Sacrifice activated. Use '!endsacrifice' before you activate it again.");
                            return;
                        }

                        if (targetUser!.SacrificeProvider != null)
                        {
                            await e.Message.RespondAsync("Target user already has a Sacrifice bound to them.");
                            return;
                        }

                        if (!_minefieldService.CanAssignSacrifice(user, targetUser))
                        {
                            await e.Message.RespondAsync("You can't become a sacrifice for this target. It would form a sacrifice loop.");
                            return;
                        }

                        var sacrificeActivated = await _minefieldService.ActivateSacrificeAsync(user, targetUser);
                        if (sacrificeActivated)
                        {
                            var sacrificeName = (await client.GetUserAsync(user.SacrificeTarget!.UserId)).Username;
                            await e.Message.RespondAsync($":sheep: Sacrifice activated! Next time {sacrificeName} would be blown up by a mine, you will be blown up instead. :sheep:");
                        }
                        else
                        {
                            await e.Message.RespondAsync("You don't have enough MF$ to activate Sacrifice.");
                        }
                        break;
                    }
                // SYMBIOTE
                case "!symbiote":
                    {
                        if (user.SymbioteTarget != null)
                        {
                            await e.Message.RespondAsync("You already have Symbiote activated. Use '!endsymbiote' before you activate it again.");
                            return;
                        }

                        if (user == targetUser)
                        {
                            await e.Message.RespondAsync("You can't target yourself");
                            return;
                        }

                        if (targetUser!.SymbioteProvider != null)
                        {
                            await e.Message.RespondAsync("Target user already has a Symbiote bound to them.");
                            return;
                        }

                        var symbioteActivated = await _minefieldService.ActivateSymbioteAsync(user, targetUser!);
                        if (symbioteActivated)
                        {
                            var symbiotename = (await client.GetUserAsync(user.SymbioteTarget!.UserId)).Username;
                            await e.Message.RespondAsync($":link: Symbiote activated! You have bound yourself to {symbiotename} and you will both receive the earnings of their next 5 messages. :link:");
                        }
                        else
                        {
                            await e.Message.RespondAsync("You don't have enough MF$ to activate Symbiote.");
                        }
                        break;
                    }
                default:
                    await e.Message.RespondAsync("Unknown command. Try `!help`.");
                    break;
            }
        }

        private async Task SendStatusEmbedAsync(DiscordClient client, MessageCreateEventArgs e, MinefieldUser targetUser)
        {
            ulong targetId = targetUser.UserId;

            var discordUser = await client.GetUserAsync(targetId);

            string lifelineName = "";
            string sacrificeName = "";
            string symbioteName = "";

            var lifeline = await _userService.GetUserAsync(targetUser.LifelineTarget?.UserId, targetUser.ServerId);
            if (lifeline != null)
            {
                lifelineName = (await client.GetUserAsync(lifeline.UserId)).Username;
            }

            var sacrifice = await _userService.GetUserAsync(targetUser.SacrificeTarget?.UserId, targetUser.ServerId);
            if (sacrifice != null)
            {
                sacrificeName = (await client.GetUserAsync(sacrifice.UserId)).Username;
            }

            var symbiote = await _userService.GetUserAsync(targetUser.SymbioteTarget?.UserId, targetUser.ServerId);
            if (symbiote != null) 
            {
                symbioteName = (await client.GetUserAsync(symbiote.UserId)).Username;
            }

            string perkString =
                $"{(targetUser.AegisCharges > 0 ? ":shield:" : "")}" +
                $"{(targetUser.HasGuardian ? ":angel:" : "")}" +
                $"{(lifeline != null ? ":drop_of_blood:" : "")}" +
                $"{(targetUser.LuckCharges > 0 ? ":four_leaf_clover:" : "")}" +
                $"{(sacrifice != null ? ":sheep:" : "")}" +
                $"{(symbiote != null ? ":link:" : "")}";

            var embed = new DiscordEmbedBuilder()
                .WithTitle($"{(targetUser.IsAlive ? ":heart:" : ":headstone:")} {discordUser.Username} {perkString}")
                .WithColor(targetUser.IsAlive ? DiscordColor.Green : DiscordColor.Red)
                .AddField("__Stats__",
                    $"\t• **Odds:** 1 in {targetUser.CurrentOdds}\n" +
                    $"\t• **MF$:** {targetUser.Currency:N0}\n" +
                    $"\t• **Streak:** {targetUser.CurrentStreak}\n" +
                    $"\t• **Total Messages:** {targetUser.TotalMessages}\n" +
                    $"\t• **Lifetime MF$:** {targetUser.LifetimeCurrency:N0}");

            var perkDetails = new List<string>();
            if (targetUser.AegisCharges > 0) perkDetails.Add($"\t• :shield: **Aegis Messages Remaining:** {targetUser.AegisCharges}");
            if (targetUser.HasGuardian) perkDetails.Add($"\t• :angel: **Guardian:** Active");
            if (targetUser.LifelineTarget != null) perkDetails.Add($"\t• :drop_of_blood: **Lifeline Target:** {lifelineName} ({targetUser.LifelineCharges} messages remaining)");
            if (targetUser.LuckCharges > 0) perkDetails.Add($"\t• :four_leaf_clover: **Luck Messages Remaining:** {targetUser.LuckCharges}");
            if (targetUser.SacrificeTarget != null) perkDetails.Add($"\t• :sheep: **Sacrifice Target:** {sacrificeName}");
            if (targetUser.SymbioteTarget != null) perkDetails.Add($"\t• :link: **Symbiote Target:** {symbioteName} ({targetUser.SymbioteCharges} messages remaining)");

            if (perkDetails.Count > 0)
                embed.AddField("__Perks__", string.Join("\n", perkDetails));

            await e.Message.RespondAsync(embed: embed);
        }

        private async Task SendHelpEmbedAsync(MessageCreateEventArgs e)
        {
            var helpEmbed = new DiscordEmbedBuilder()
                .WithTitle(":bulb: Minefield Commands :bulb:")
                .WithColor(DiscordColor.Rose)
                .AddField("__Perks__",
                    $"\t• **!aegis** — Your next 5 messages are protected from mines. Does not affect Sacrifice. (20 message cooldown)\n" +
                    $"\t• **!fortune** — Improve your current odds by 5. (up to a maximum of 50)\n" +
                    $"\t• **!guardian** — Negate the effects of the next mine you trigger, but reset your streak. In Sacrifice chains, Guardian protects the first user in the chain; otherwise, it triggers last. (30 message cooldown)\n" +
                    $"\t• **!lifeline <username>** — Revive a dead user, resetting their odds and streak. You both receive the earnings of their next 10 messages.\n" +
                    $"\t• **!luck** — Double the earnings of your next 5 messages.\n" +
                    $"\t• **!sacrifice <username>** — The next time your target would be blown up by a mine, you are blown up in their place, however, their streak is still reset. You receive MF$ equal to 10% of the target's current MF$. Sacrifice can chain, however, it cannot form a loop.\n" +
                    $"\t• **!symbiote <username>** — Bind yourself to a living user. You both receive the earnings of their next 5 messages."
                )
                .AddField("__Perks (continued.)__", 
                    $"\t• **!endlifeline** — Unbinds you from your lifeline target. You will no longer receive the earnings of their messages.\n" +
                    $"\t• **!endsacrifice** — Unbinds you from your sacrifice target. You will no longer blow up in their place.\n" +
                    $"\t• **!endsymbiote** — Unbinds you from your symbiote target. You will no longer receive the earnings of their messages.\n"
                )
                .AddField("__Utility__",
                    $"\t• **!balance [username]** — Show a user's current MF$.\n" +
                    $"\t• **!help** — Show this help message.\n" +
                    $"\t• **!messages [username]** — Show how many messages a user has sent.\n" +
                    $"\t• **!odds [username]** — Show a user's current odds.\n" +
                    $"\t• **!perks** — Show all perk prices and descriptions.\n" +
                    $"\t• **!status [username]** — View a user's active perks, current streak, odds, and MF$.\n" +
                    $"\t• **!streak [username]** — Show a user's current streak.");

            await e.Message.RespondAsync(embed: helpEmbed);
        }

        private async Task SendPerkEmbedAsync(MessageCreateEventArgs e)
        {
            var perkEmbed = new DiscordEmbedBuilder()
                .WithTitle(":star: Minefield Perks :star:")
                .WithColor(DiscordColor.Azure)
                .AddField($":shield: Aegis ({_minefieldService.perkCosts["aegis"]} MF$)", "Your next 5 messages are protected from mines. Does not affect Sacrifice. (20 message cooldown)", false)
                .AddField($":chart_with_upwards_trend: Fortune ({_minefieldService.perkCosts["fortune"]} MF$)", "Improve your current odds by 5. (up to a maximum of 50)", false)
                .AddField($":angel: Guardian ({_minefieldService.perkCosts["guardian"]} MF$)", "Negate the effects of the next mine you trigger. In Sacrifice chains, Guardian protects the first user in the chain; otherwise, it triggers last. (30 message cooldown)", false)
                .AddField($":drop_of_blood: Lifeline ({_minefieldService.perkCosts["lifeline"]} MF$)", "Revive a dead user, resetting their odds and streak. You both receive the earnings of their next 10 messages.", false)
                .AddField($":four_leaf_clover: Luck ({_minefieldService.perkCosts["luck"]} MF$)", "Double the earnings of your next 5 messages.", false)
                .AddField($":sheep: Sacrifice ({_minefieldService.perkCosts["sacrifice"]} MF$)", "The next time your target would be blown up by a mine, you are blown up in their place. You receive MF$ equal to 10% of the target's current MF$. Sacrifice can chain, however, it cannot form a loop.", false)
                .AddField($":link: Symbiote ({_minefieldService.perkCosts["symbiote"]} MF$)", "Bind yourself to a living user. You both receive the earnings of their next 5 messages.", false);

            await e.Message.RespondAsync(embed: perkEmbed);
        }

        private async Task SendLeaderboardEmbedAsync(MessageCreateEventArgs e, List<LeaderboardEntry> leaderboardEntries)
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

            await e.Message.RespondAsync(embed: leaderboardEmbed);
        }
    }

    public class LeaderboardEntry
    {
        public string Name { get; set; } = string.Empty;
        public int Currency { get; set; }
    }
}
