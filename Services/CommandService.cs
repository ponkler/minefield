using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity.Extensions;
using Minefield.Entities;

namespace Minefield.Services
{
    public class CommandService
    {
        private readonly UserService _userService;
        private readonly MinefieldService _minefieldService;
        private readonly Random _rng = new Random();

        public CommandService(UserService userService, MinefieldService minefieldService)
        {
            _userService = userService;
            _minefieldService = minefieldService;
        }

        public async Task HandleCommandAsync(DiscordClient client, MessageCreateEventArgs e)
        {
            // The User who entered the command
            MinefieldUser user = await _userService.GetOrCreateUserAsync(e.Author.Id, e.Guild.Id, e.Author.Username);
            var server = await client.GetGuildAsync(user.ServerId);
            var discUser = await server.GetMemberAsync(user.UserId);
            bool isJanitor = discUser.Roles.Any(r => r.Name == "Minefield Janitor");

            // The User specified in the command argument (if any)
            MinefieldUser? targetUser = null;

            string[] parts = e.Message.Content.Trim().Split(' ');
            string command = parts[0].ToLower();

            string[]? args = parts.Skip(1).Select(p => p.Trim()).ToArray();

            bool parsedArg = false;
            int argInt = -1;

            if (args.Length > 0)
            {
                DiscordMember? member = (await e.Guild.GetAllMembersAsync()).FirstOrDefault(
                        m => string.Equals(m.Username, args[0], StringComparison.OrdinalIgnoreCase)
                    );

                // arg 1 was not valid user
                if (member == null)
                {
                    parsedArg = Int32.TryParse(args[0], out argInt);

                    // arg 1 was not int either
                    if (!parsedArg)
                    {
                        await e.Message.RespondAsync($"Could not find user {args[0]}");
                        return;
                    }
                }
                else
                {
                    // arg 1 was valid user
                    targetUser = await _userService.GetOrCreateUserAsync(member.Id, member.Guild.Id, member.Username);

                    // WE ALSO NEED TO CHECK IF ARG 1 IS AN INT SINCE DISCORD ALLOWS USERNAMES TO BE JUST NUMBERS
                    parsedArg = Int32.TryParse(args[0], out argInt);

                    // IF SECOND ARG EXISTS FIRST ARG SHOULD NEVER BE INT BUT CAN STILL BE USER WHYYYYYYY
                    if (args.Length == 2 && targetUser != null)
                    {
                        parsedArg = Int32.TryParse(args[1], out argInt);

                        if (!parsedArg)
                        {
                            await e.Message.RespondAsync($"Invalid arguments.");
                            return;
                        }
                    }
                }

                if (args.Length > 2)
                {
                    await e.Message.RespondAsync($"Invalid arguments.");
                    return;
                }
            }

            switch (command)
            {
                // A ======================================================================================================
                case "!aegis":
                    {
                        if (args.Length != 0)
                        {
                            await e.Message.RespondAsync("Invalid arguments.");
                            return;
                        }

                        if (user.AegisCharges > 0)
                        {
                            await e.Message.RespondAsync($"Aegis is already activated. You have {user.AegisCharges} protected messages remaining.");
                            return;
                        }

                        // 15 message cooldown after aegis ends
                        if (user.MessagesSinceAegis < 20)
                        {
                            await e.Message.RespondAsync($"You must send {20 - (user.MessagesSinceAegis)} more messages before you can activate Aegis again.");
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
                    }
                    break;
                case "!arena":
                    {
                        if (!parsedArg || args.Length != 1)
                        {
                            await e.Message.RespondAsync("Invalid arguments.");
                            return;
                        }

                        if (_minefieldService.ArenaActive())
                        {
                            await e.Message.RespondAsync($"There is already an active Arena.");
                            return;
                        }

                        if (argInt <= 0)
                        {
                            await e.Message.RespondAsync($"Arena buy in must be positive.");
                            return;
                        }

                        if (user.Currency < argInt)
                        {
                            await e.Message.RespondAsync($"You don't have enough MF$ to start this Arena.");
                            return;
                        }

                        await SendArenaStartedEmbedAsync(client, e, user, argInt);

                        var arenaCreated = await _minefieldService.SetUpArenaAsync(argInt, user);

                        if (!arenaCreated)
                        {
                            await SendArenaCancelledEmbedAsync(client, e);
                            return;
                        }
                        
                    }
                    break;
                // B ======================================================================================================
                case "!balance":
                    {
                        if (args.Length > 1 || (parsedArg && targetUser == null))
                        {
                            await e.Message.RespondAsync("Invalid arguments.");
                            return;
                        }

                        await e.Message.RespondAsync($"{(targetUser ?? user).Username} has {(targetUser ?? user).Currency:N0} MF$.");
                    }
                    break;
                // C ======================================================================================================

                // D ======================================================================================================
                case "!deathpact":
                    {
                        if (args.Length > 1 || (parsedArg && targetUser == null))
                        {
                            await e.Message.RespondAsync("Invalid arguments.");
                            return;
                        }

                        if (targetUser == null)
                        {
                            await e.Message.RespondAsync("You must specify a target.");
                            return;
                        }

                        if (user == targetUser)
                        {
                            await e.Message.RespondAsync("You can't target yourself");
                            return;
                        }

                        if (user.DeathPactTarget != null)
                        {
                            await e.Message.RespondAsync("You already have a Death Pact. Use '!enddeathpact' before you activate it again.");
                            return;
                        }

                        if (!targetUser.IsAlive)
                        {
                            await e.Message.RespondAsync("Target user is dead. You can only make Death Pacts with living users.");
                            return;
                        }

                        if (targetUser.DeathPactTarget != null)
                        {
                            await e.Message.RespondAsync("Target user already has a Death Pact.");
                            return;
                        }

                        if (user.Currency < _minefieldService.perkCosts["death_pact"])
                        {
                            await e.Message.RespondAsync("You don't have enough MF$ to activate a Death Pact.");
                            return;
                        }

                        if (targetUser.Currency < _minefieldService.perkCosts["death_pact"])
                        {
                            await e.Message.RespondAsync("Target user doesn't have enough MF$ to activate a Death Pact.");
                            return;
                        }

                        if ((await _userService.GetLinkedUsers(user)).Contains(targetUser))
                        {
                            await e.Message.RespondAsync("You are already linked to that user with another perk.");
                            return;
                        }

                        var deathPactAccepted = await SendDeathPactEmbedAsync(client, e, targetUser);
                        if (!deathPactAccepted)
                        {
                            return;
                        }

                        var deathPactActivated = await _minefieldService.ActivateDeathPactAsync(user, targetUser);
                        if (deathPactActivated)
                        {
                            await e.Message.RespondAsync(":scroll: Death Pact activated! You both receive each others earnings. If one of you blows up, the other will blow up with them. On death, you will lose 10 max odds instead of 5. :scroll:");
                        }
                    }
                    break;
                // E ======================================================================================================
                case "!endlifeline":
                    {
                        if (args.Length != 0)
                        {
                            await e.Message.RespondAsync("Invalid arguments.");
                            return;
                        }

                        if (user.LifelineTarget == null)
                        {
                            await e.Message.RespondAsync("You don't have a Lifeline activated.");
                            return;
                        }

                        var lifelineName = user.LifelineTarget.Username;
                        await _minefieldService.RemoveLifelineAsync(user);

                        await e.Message.RespondAsync($"You have ended your Lifeline with {lifelineName}");
                        
                    }
                    break;
                case "!endsacrifice":
                    {
                        if (args.Length != 0)
                        {
                            await e.Message.RespondAsync("Invalid arguments.");
                            return;
                        }

                        if (user.SacrificeTarget == null)
                        {
                            await e.Message.RespondAsync("You don't have a Sacrifice activated.");
                            return;
                        }

                        var sacrificeName = user.SacrificeTarget.Username;
                        await _minefieldService.RemoveSacrificeAsync(user);

                        await e.Message.RespondAsync($"You have ended your Sacrifice for {sacrificeName}");
                        
                    }
                    break;
                case "!endsymbiote":
                    {
                        if (args.Length != 0)
                        {
                            await e.Message.RespondAsync("Invalid arguments.");
                            return;
                        }

                        if (user.SymbioteTarget == null)
                        {
                            await e.Message.RespondAsync("You don't have a Symbiote activated.");
                            return;
                        }

                        var symbioteName = user.SymbioteTarget.Username;
                        await _minefieldService.RemoveSymbioteAsync(user);

                        await e.Message.RespondAsync($"You have ended your Symbiote for {symbioteName}");
                        
                    }
                    break;
                // F ======================================================================================================
                case "!fortune":
                    {
                        if (args.Length != 0)
                        {
                            await e.Message.RespondAsync("Invalid arguments.");
                            return;
                        }

                        if (user.FortuneCharges > 0)
                        {
                            await e.Message.RespondAsync($"You already have Fortune activated. You have {user.FortuneCharges} Fortune messages remaining");
                            return;
                        }

                        var fortuneActivated = await _minefieldService.ActivateFortuneAsync(user);
                        if (fortuneActivated)
                        {
                            await e.Message.RespondAsync(":coin: Fortune activated! The earnings of your next 5 messages are doubled. :coin:");
                        }
                        else
                        {
                            await e.Message.RespondAsync("You don't have enough MF$ to activate Fortune.");
                        }
                    }
                    break;
                // G ======================================================================================================
                case "!guardian":
                    {
                        if (args.Length != 0)
                        {
                            await e.Message.RespondAsync("Invalid arguments.");
                            return;
                        }

                        if (user.HasGuardian)
                        {
                            await e.Message.RespondAsync("You already have Guardian activated.");
                            return;
                        }

                        // 15 message cooldown after activating guardian
                        if (user.MessagesSinceGuardian < 15)
                        {
                            await e.Message.RespondAsync($"You must send {15 - (user.MessagesSinceGuardian)} more messages before you can activate Guardian again.");
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
                    }
                    break;
                // H ======================================================================================================
                case "!help":
                    {
                        if (args.Length != 0)
                        {
                            await e.Message.RespondAsync("Invalid arguments.");
                            return;
                        }

                        await SendHelpEmbedAsync(e);
                    }
                    break;
                // I ======================================================================================================
                case "!info":
                    {
                        if (args.Length != 0)
                        {
                            await e.Message.RespondAsync("Invalid arguments.");
                            return;
                        }

                        await SendInfoEmbedAsync(e);
                    }
                    break;
                // J ======================================================================================================
                case "!join":
                    {
                        if (args.Length != 0)
                        {
                            await e.Message.RespondAsync("Invalid arguments.");
                            return;
                        }

                        if (!_minefieldService.ArenaActive())
                        {
                            await e.Message.RespondAsync($"There is no Arena to join.");
                            return;
                        }

                        if (_minefieldService.IsInArena(user))
                        {
                            await e.Message.RespondAsync($"You are already in the Arena.");
                            return;
                        }

                        if (!_minefieldService.CanJoinArena(user))
                        {
                            await e.Message.RespondAsync($"You don't have enough MF$ to join this Arena.");
                            return;
                        }

                        await _minefieldService.AddUserToArenaAsync(user);
                        await e.Message.RespondAsync($":crossed_swords: You have joined the Arena! :crossed_swords:");

                    }
                    break;
                // K ======================================================================================================

                // L ======================================================================================================
                case "!leaderboard":
                    {
                        if (args.Length != 0)
                        {
                            await e.Message.RespondAsync("Invalid arguments.");
                            return;
                        }

                        var leaderboard = await _userService.GetLeaderboardAsync(user.ServerId);
                        List<LeaderboardEntry> entries = new List<LeaderboardEntry>();

                        foreach (var entry in leaderboard)
                        {
                            LeaderboardEntry newEntry = new LeaderboardEntry
                            {
                                Name = entry.Username,
                                Currency = entry.Currency
                            };

                            entries.Add(newEntry);
                        }

                        await SendLeaderboardEmbedAsync(e, entries);
                    }
                    break;
                case "!lifeline":
                    {
                        if (args.Length > 1 || (parsedArg && targetUser == null))
                        {
                            await e.Message.RespondAsync("Invalid arguments.");
                            return;
                        }

                        if (targetUser == null)
                        {
                            await e.Message.RespondAsync("You must specify a target.");
                            return;
                        }

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

                        if (targetUser!.IsAlive)
                        {
                            await e.Message.RespondAsync("Target user is still alive. Lifeline can only be used on dead users.");
                            return;
                        }

                        if (targetUser.LifelineProvider != null)
                        {
                            await e.Message.RespondAsync("Target user already has a Lifeline bound to them.");
                            return;
                        }

                        if ((await _userService.GetLinkedUsers(user)).Contains(targetUser))
                        {
                            await e.Message.RespondAsync("You are already linked to that user with another perk.");
                            return;
                        }

                        var lifelineActivated = await _minefieldService.ActivateLifelineAsync(user, targetUser);
                        if (lifelineActivated)
                        {
                            var lifelineName = user.LifelineTarget!.Username;
                            await e.Message.RespondAsync($":drop_of_blood: Lifeline activated! You have revived {lifelineName} and you will both receive the earnings of their next 10 messages. :drop_of_blood:");
                        }
                        else
                        {
                            await e.Message.RespondAsync("You don't have enough MF$ to activate Lifeline.");
                        }
                        
                    }
                    break;
                case "!luck":
                    {
                        if (args.Length != 1)
                        {
                            await e.Message.RespondAsync("Invalid arguments.");
                            return;
                        }

                        if (user.CurrentOdds == user.MaxOdds)
                        {
                            await e.Message.RespondAsync("Your odds can't be improved any further.");
                            return;
                        }

                        if (argInt <= 0)
                        {
                            await e.Message.RespondAsync("Amount must be greater than 0.");
                            return;
                        }

                        bool luckActivated = await _minefieldService.ActivateLuckAsync(user, argInt);
                        if (luckActivated)
                        {
                            await e.Message.RespondAsync($":four_leaf_clover: Luck activated! Your odds have been improved to 1 in {user.CurrentOdds}. :four_leaf_clover:");
                        }
                        else
                        {
                            await e.Message.RespondAsync("You don't have enough MF$ to activate Luck.");
                        }
                    }
                    break;
                // M ======================================================================================================
                case "!maxodds":
                    {
                        if (args.Length > 1)
                        {
                            await e.Message.RespondAsync("Invalid arguments.");
                            return;
                        }

                        await e.Message.RespondAsync($"{(targetUser ?? user).Username}'s max odds are 1 in {(targetUser ?? user).MaxOdds}.");
                    }
                    break;
                case "!messages":
                    {
                        if (args.Length > 1)
                        {
                            await e.Message.RespondAsync("Invalid arguments.");
                            return;
                        }

                        await e.Message.RespondAsync($"{(targetUser ?? user).Username} has sent {(targetUser ?? user).TotalMessages:N0} messages.");
                    }
                    break;
                // N ======================================================================================================

                // O ======================================================================================================
                case "!odds":
                    {
                        if (args.Length > 1)
                        {
                            await e.Message.RespondAsync("Invalid arguments.");
                            return;
                        }

                        await e.Message.RespondAsync($"{(targetUser ?? user).Username}'s odds are 1 in {(targetUser ?? user).CurrentOdds}.");
                    }
                    break;
                // P ======================================================================================================
                case "!perks":
                    {
                        if (args.Length != 0)
                        {
                            await e.Message.RespondAsync("Invalid arguments.");
                            return;
                        }

                        await SendPerkEmbedAsync(e);
                    }
                    break;
                // Q ======================================================================================================

                // R ======================================================================================================
                case "!reset":
                    {
                        if (isJanitor)
                        {
                            if (args.Length > 1)
                            {
                                await e.Message.RespondAsync("Invalid arguments.");
                                return;
                            }

                            var resetUser = await _userService.ResetUserAsync((targetUser ?? user).UserId, (targetUser ?? user).ServerId);

                            var username = (await client.GetUserAsync((targetUser ?? user).UserId)).Username;

                            await e.Message.RespondAsync($"{username}'s progress has been reset.");
                            await _userService.SaveAsync();
                        }
                    }
                    break;
                case "!restore":
                    {
                        if (args.Length != 1 || !parsedArg)
                        {
                            await e.Message.RespondAsync("Invalid arguments.");
                            return;
                        }

                        if (user.MaxOdds == 50)
                        {
                            await e.Message.RespondAsync("Your max odds can't be improved any further.");
                            return;
                        }

                        if (argInt <= 0)
                        {
                            await e.Message.RespondAsync("Amount must be greater than 0.");
                            return;
                        }

                        bool restoreActivated = await _minefieldService.ActivateRestoreAsync(user, argInt);
                        if (restoreActivated)
                        {
                            await e.Message.RespondAsync($":adhesive_bandage: Restore activated! Your max odds have been improved to 1 in {user.MaxOdds}. :adhesive_bandage:");
                        }
                        else
                        {
                            await e.Message.RespondAsync("You don't have enough MF$ to activate Restore.");
                        }
                    }
                    break;
                case "!revive":
                    {
                        if (isJanitor)
                        {
                            if (args.Length > 1 || (parsedArg && targetUser == null))
                            {
                                await e.Message.RespondAsync("Invalid arguments.");
                                return;
                            }

                            if ((targetUser ?? user).IsAlive)
                            {
                                await e.Message.RespondAsync("That user is already alive.");
                                return;
                            }

                            await _minefieldService.ReviveUser((targetUser ?? user));
                            await e.Message.RespondAsync($"{(targetUser ?? user).Username} has been revived.");
                        }
                    }
                    break;
                // S ======================================================================================================
                case "!sacrifice":
                    {
                        if (args.Length > 1 || (parsedArg && targetUser == null))
                        {
                            await e.Message.RespondAsync("Invalid arguments.");
                            return;
                        }

                        if (targetUser == null)
                        {
                            await e.Message.RespondAsync("You must specify a target.");
                            return;
                        }

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

                        if ((await _userService.GetLinkedUsers(user)).Contains(targetUser))
                        {
                            await e.Message.RespondAsync("You are already linked to that user with another perk.");
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
                        
                    }
                    break;
                case "!setbalance":
                    {
                        if (isJanitor)
                        {
                            if (args.Length == 1 && !parsedArg)
                            {
                                await e.Message.RespondAsync("Invalid arguments.");
                                return;
                            }

                            if (args.Length == 2 && (!parsedArg || targetUser == null))
                            {
                                await e.Message.RespondAsync("Invalid arguments.");
                                return;
                            }

                            if (parsedArg && targetUser == null)
                            {
                                user.Currency = argInt;
                                await e.Message.RespondAsync($"Set {user.Username}'s balance to {argInt} MF$.");
                                await _userService.SaveAsync();
                            }

                            if (parsedArg && targetUser != null)
                            {
                                targetUser.Currency = argInt;
                                await e.Message.RespondAsync($"Set {targetUser.Username}'s balance to {argInt} MF$.");
                                await _userService.SaveAsync();
                            }
                        }
                    }
                    break;
                case "!status":
                    {
                        if (args.Length > 1 || (parsedArg && targetUser == null))
                        {
                            await e.Message.RespondAsync("Invalid arguments.");
                            return;
                        }

                        await SendStatusEmbedAsync(client, e, targetUser ?? user);
                    }
                    break;
                case "!streak":
                    {
                        if (args.Length > 1 || (parsedArg && targetUser == null))
                        {
                            await e.Message.RespondAsync("Invalid arguments.");
                            return;
                        }

                        await e.Message.RespondAsync($"{(targetUser ?? user).Username} is currently on a streak of {(targetUser ?? user).CurrentStreak:N0}.");
                    }
                    break;
                case "!symbiote":
                    {
                        if (args.Length > 1)
                        {
                            await e.Message.RespondAsync("Too many arguments.");
                            return;
                        }

                        if (user.SymbioteTarget != null)
                        {
                            await e.Message.RespondAsync("You already have Symbiote activated. Use '!endsymbiote' before you activate it again.");
                            return;
                        }

                        if (targetUser == null)
                        {
                            await e.Message.RespondAsync("You must specify a target.");
                            return;
                        }

                        if (parsedArg)
                        {
                            await e.Message.RespondAsync("Invalid argument.");
                            return;
                        }

                        if (user == targetUser)
                        {
                            await e.Message.RespondAsync("You can't target yourself");
                            return;
                        }

                        if (targetUser.SymbioteProvider != null)
                        {
                            await e.Message.RespondAsync("Target user already has a Symbiote bound to them.");
                            return;
                        }

                        if ((await _userService.GetLinkedUsers(user)).Contains(targetUser))
                        {
                            await e.Message.RespondAsync("You are already linked to that user with another perk.");
                            return;
                        }

                        var symbioteActivated = await _minefieldService.ActivateSymbioteAsync(user, targetUser);
                        if (symbioteActivated)
                        {
                            var symbiotename = (await client.GetUserAsync(user.SymbioteTarget!.UserId)).Username;
                            await e.Message.RespondAsync($":link: Symbiote activated! You have bound yourself to {symbiotename} and you will both receive the earnings of their next 5 messages. :link:");
                        }
                        else
                        {
                            await e.Message.RespondAsync("You don't have enough MF$ to activate Symbiote.");
                        }

                    }
                    break;
                // T ======================================================================================================

                // U ======================================================================================================
                case "!users":
                    {
                        if (args.Length != 0)
                        {
                            await e.Message.RespondAsync("Invalid arguments.");
                            return;
                        }

                        await SendUsersEmbedAsync(e);
                    }
                    break;
                // V ======================================================================================================

                // W ======================================================================================================

                // X ======================================================================================================

                // Y ======================================================================================================

                // Z ======================================================================================================

                default:
                    await e.Message.RespondAsync("Unknown command. Try `!help`.");
                    break;
            }
        }

        public async Task SendArenaStartedEmbedAsync(DiscordClient client, MessageCreateEventArgs e, MinefieldUser host, int buyIn)
        {
            var name = (await client.GetUserAsync(host.UserId)).Username;

            var arenaStartedEmbed = new DiscordEmbedBuilder()
                .WithTitle(":rotating_light: Arena Started :rotating_light:")
                .WithColor(DiscordColor.IndianRed)
                .AddField("__Details__", $"{name} has started an Arena which will begin in 60 seconds. Type \"!join\" to join. The buy in is {buyIn:N0} MF$.", false);

            await e.Message.RespondAsync(embed: arenaStartedEmbed);
        }

        public async Task SendArenaCancelledEmbedAsync(DiscordClient client, MessageCreateEventArgs e)
        {
            var arenaCancelledEmbed = new DiscordEmbedBuilder()
                .WithTitle(":x: Arena Cancelled :x:")
                .WithColor(DiscordColor.IndianRed)
                .AddField("__Details__", $"Nobody joined the arena. The buy in has been refunded.", false);

            await e.Message.RespondAsync(embed: arenaCancelledEmbed);
        }

        public async Task SendArenaRoundEmbedAsync(DiscordClient client, DiscordChannel channel, Dictionary<MinefieldUser, int> participantRolls, int round)
        {
            List<string> rollStrings = new List<string>();

            foreach (var entry in participantRolls) 
            {
                var name = (await client.GetUserAsync(entry.Key.UserId)).Username;
                string str = $"\t• {name} - {entry.Value}/5 {(entry.Value == 5 ? ":boom:" : ":ok:")}";
                rollStrings.Add(str);
            }

            var arenaRoundEmbed = new DiscordEmbedBuilder()
                .WithTitle($":crossed_swords: Arena Round {round} :crossed_swords:")
                .WithColor(DiscordColor.IndianRed)
                .AddField("__Rolls__", string.Join("\n", rollStrings), false);

            await channel.SendMessageAsync(embed: arenaRoundEmbed);
        }

        public async Task SendArenaParticipantsEmbedAsync(DiscordClient client, DiscordChannel channel, List<MinefieldUser> participants, int payout)
        {
            List<string> users = new List<string>();

            foreach (var participant in participants)
            {
                string str = $"\t• {participant.Username}";
                users.Add(str);
            }

            var arenaResolveEmbed = new DiscordEmbedBuilder()
                .WithTitle(":dagger: Arena Participants :dagger:")
                .WithColor(DiscordColor.IndianRed)
                .AddField("__List__", string.Join("\n", users), false)
                .AddField("__Payout__", $"\t• {payout:N0} MF$");

            await channel.SendMessageAsync(embed: arenaResolveEmbed);
        }

        public async Task SendArenaResolveEmbedAsync(DiscordClient client, DiscordChannel channel, List<MinefieldUser> winners, int payout)
        {
            int split = (int)(payout / winners.Count);

            List<string> payoutStrings = new List<string>();

            foreach (var winner in winners)
            {
                var name = (await client.GetUserAsync(winner.UserId)).Username;
                string str = $"\t• {name} - {split:N0} MF$";
                payoutStrings.Add(str);
            }

            var arenaResolveEmbed = new DiscordEmbedBuilder()
                .WithTitle(":trophy: Arena Winners :trophy:")
                .WithColor(DiscordColor.Gold)
                .AddField("__Winners__", string.Join("\n", payoutStrings), false);

            await channel.SendMessageAsync(embed: arenaResolveEmbed);
        }

        private async Task<bool> SendDeathPactEmbedAsync(DiscordClient client, MessageCreateEventArgs e, MinefieldUser targetUser)
        {
            var deathPactEmbed = new DiscordEmbedBuilder()
                .WithTitle(":scroll: Death Pact :scroll:")
                .WithColor(DiscordColor.Black)
                .AddField("__Offer__", $"{targetUser.Username}, {e.Author.Username} has offered you a Death Pact. If you agree, you and {e.Author.Username} " +
                $"will share message earnings, but if either of you trigger a mine, you will both blow up and lose 10 max odds instead of 5.", false)
                .AddField("__Options__", $"\t• React :pen_fountain: to accept the Death Pact offer.\n" +
                $"\t• React :x: to decline the Death Pact offer.");

            var member = await client.GetUserAsync(targetUser.UserId);
            var penEmoji = DiscordEmoji.FromName(client, ":pen_fountain:");
            var cancelEmoji = DiscordEmoji.FromName(client, ":x:");
            var message = await client.SendMessageAsync(e.Channel, embed: deathPactEmbed);

            await message.CreateReactionAsync(penEmoji);
            await message.CreateReactionAsync(cancelEmoji);

            var interactivity = client.GetInteractivity();

            var reactionResult = await interactivity.WaitForReactionAsync(
                x => x.Message == message
                     && x.User == member
                     && (x.Emoji == penEmoji || x.Emoji == cancelEmoji)
            );

            if (reactionResult.TimedOut)
            {
                await e.Message.RespondAsync($"{targetUser.Username} did not respond to your Death Pact offer.");
                return false;
            }
            else if (reactionResult.Result.Emoji == penEmoji) 
            {
                return true;
            }
            else if (reactionResult.Result.Emoji == cancelEmoji)
            {
                await e.Message.RespondAsync($"{targetUser.Username} declined your Death Pact offer.");
                return false;
            }

            return false;
        }

        private async Task SendInfoEmbedAsync(MessageCreateEventArgs e)
        {
            var infoEmbed = new DiscordEmbedBuilder()
                .WithTitle(":clipboard: Minefield Information :clipboard:")
                .WithColor(DiscordColor.White)
                .AddField("__General__",
                    $"\t• The minefield is a channel where your messages have a chance to explode and kill you, but you can buy useful perks to prevent this.\n" +
                    $"\t• Exploding in the minefield removes your permissions to view and send messages in the channel. It also lowers your maximum odds by 5 and resets your streak and linked perks.\n" +
                    $"\t• Everybody starts with a 1 in 50 chance that their message will trigger a mine. This reduces with each message sent, to 1 in 49, 1 in 48, etc.\n" +
                    $"\t• Every message you send increases your streak by 1, and you earn Minefield Dollars (MF$) equal to your current streak when you send a message.\n" +
                    $"\t• Sending a command does not roll for a mine or increase your streak.\n" +
                    $"\t• You can buy perks to increase your earnings, or try to survive longer. To see these, use the \"!perks\" command.\n"
                )
                .AddField("__Tips__",
                    $"\t• Aegis and Guardian cooldowns don't begin until the perk has been used up.\n" +
                    $"\t• You may only have one link to a user. So, if you have a Sacrifice linked to a user, you cannot have a Symbiote linked to the same user.\n" +
                    $"\t• The best possible odds are 1 in 50. If you attempt to use Luck or Restore to go beyond this limit, the amount will automatically be capped.\n" +
                    $"\t• In a Sacrifice chain, Guardian will only be used on the final user.\n" +
                    $"\t• Death Pacts cannot be ended manually.\n" +
                    $"\t• If you have Guardian activated, and your Death Pact partner triggers a mine, your Guardian will not defend you. If your partner has Guardian, and they trigger a mine, their Guardian will protect you both.\n" +
                    $"\t• The above is also true for Sacrifice. If your Death Pact partner triggers a mine, but you have a Sacrifice linked to you, your Sacrifice will not save you, but your partners will save you both.\n" +
                    $"\t• With Guardian active, triggering a mine won't remove you from the channel, but it will still lower your maximum odds, and will cut your streak in half. This applies in Death Pacts and Sacrifice chains."
                );

            await e.Message.RespondAsync(embed: infoEmbed);
        }

        private async Task SendStatusEmbedAsync(DiscordClient client, MessageCreateEventArgs e, MinefieldUser targetUser)
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
                .WithTitle($"{(targetUser.IsAlive ? ":heart:" : ":headstone:")} {targetUser.Username} {perkString}")
                .WithColor(targetUser.IsAlive ? DiscordColor.Green : DiscordColor.Red)
                .AddField("__Stats__",
                    $"\t• **Odds:** 1 in {targetUser.CurrentOdds}\n" +
                    $"\t• **Max Odds:** 1 in {targetUser.MaxOdds}\n" +
                    $"\t• **MF$:** {targetUser.Currency:N0}\n" +
                    $"\t• **Streak:** {targetUser.CurrentStreak}\n" +
                    $"\t• **Total Messages:** {targetUser.TotalMessages}\n" +
                    $"\t• **Lifetime MF$:** {targetUser.LifetimeCurrency:N0}");

            var perkDetails = new List<string>();
            if (targetUser.AegisCharges > 0) perkDetails.Add($"\t• :shield: **Aegis Messages Remaining:** {targetUser.AegisCharges}");
            if (targetUser.DeathPactTarget != null) perkDetails.Add($"\t• :scroll: **Death Pact:** {targetUser.DeathPactTarget.Username}");
            if (targetUser.HasGuardian) perkDetails.Add($"\t• :angel: **Guardian:** Active");
            if (targetUser.LifelineTarget != null) perkDetails.Add($"\t• :drop_of_blood: **Lifeline Target:** {targetUser.LifelineTarget.Username} ({targetUser.LifelineCharges} messages remaining)");
            if (targetUser.FortuneCharges > 0) perkDetails.Add($"\t• :coin: **Fortune Messages Remaining:** {targetUser.FortuneCharges}");
            if (targetUser.SacrificeTarget != null) perkDetails.Add($"\t• :sheep: **Sacrifice Target:** {targetUser.SacrificeTarget.Username}");
            if (targetUser.SymbioteTarget != null) perkDetails.Add($"\t• :link: **Symbiote Target:** {targetUser.SymbioteTarget.Username} ({targetUser.SymbioteCharges} messages remaining)");

            if (perkDetails.Count > 0)
                embed.AddField("__Perks__", string.Join("\n", perkDetails));

            await e.Message.RespondAsync(embed: embed);
        }

        private async Task SendHelpEmbedAsync(MessageCreateEventArgs e)
        {
            var helpEmbed = new DiscordEmbedBuilder()
                .WithTitle(":bulb: Minefield Commands :bulb:")
                .WithColor(DiscordColor.Rose)
                .AddField("__Self Perks__",
                    $"\t• **!aegis** — Your next 5 messages are protected from mines. Does not affect Sacrifice. (20 message cooldown after depletion)\n" +
                    $"\t• **!fortune** — Double the earnings of your next 5 messages.\n" +
                    $"\t• **!guardian** — Negate the effects of the next mine you trigger, but reset your streak. In Sacrifice chains, Guardian protects the first user in the chain; otherwise, it triggers last. (15 message cooldown after depletion)\n" +
                    $"\t• **!luck** — Improve your current odds by a chosen amount. (up to your maximum)\n" +
                    $"\t• **!restore** — Increase your maximum odds, and current odds, by a chosen amount. (up to a maximum of 50)\n"
                )
                .AddField("__Linked Perks__",
                    $"\t• **!deathpact <username>** — You and the target share message earnings. When you or the target trigger a mine, you both blow up and lose 10 max odds.\n" +
                    $"\t• **!lifeline <username>** — Revive a dead user, resetting their odds and streak. You both receive the earnings of their next 10 messages.\n" +
                    $"\t• **!sacrifice <username>** — The next time your target would be blown up by a mine, you are blown up in their place, however, their streak is still reset. You receive MF$ equal to 10% of the target's current MF$. Sacrifice can chain, however, it cannot form a loop.\n" +
                    $"\t• **!symbiote <username>** — Bind yourself to a living user. You both receive the earnings of their next 5 messages.\n" +
                    $"\t• **!endlifeline** — Unbinds you from your lifeline target. You will no longer receive the earnings of their messages.\n" +
                    $"\t• **!endsacrifice** — Unbinds you from your sacrifice target. You will no longer blow up in their place.\n" +
                    $"\t• **!endsymbiote** — Unbinds you from your symbiote target. You will no longer receive the earnings of their messages.\n"
                )
                .AddField("__Events__",
                    $"\t• **!arena <amount>** — Starts an arena with the given amount as the buy in.\n" +
                    $"\t• **!join** — Joins an active arena."
                )
                .AddField("__Utility__",
                    $"\t• **!balance [username]** — Show a user's current MF$.\n" +
                    $"\t• **!help** — Show this help message.\n" +
                    $"\t• **!info** — Show general information and tips about the minefield.\n" +
                    $"\t• **!maxodds [username]** — Shows a user's max odds.\n" +
                    $"\t• **!messages [username]** — Show how many messages a user has sent.\n" +
                    $"\t• **!odds [username]** — Show a user's current odds.\n" +
                    $"\t• **!perks** — Show all perk prices and descriptions.\n" +
                    $"\t• **!status [username]** — View a user's active perks, current streak, odds, and MF$.\n" +
                    $"\t• **!streak [username]** — Show a user's current streak.\n" +
                    $"\t• **!users** — Show a list of the usernames of all users in the server."
                )
                .AddField("__Janitor Commands__",
                    $"\t• **!setbalance [username] <amount>** — Sets a user's balance to the given amount.\n" +
                    $"\t• **!reset [username]** — Wipes a user's progress.\n" +
                    $"\t• **!revive [username]** — Revives a dead user."
                );

            await e.Message.RespondAsync(embed: helpEmbed);
        }

        private async Task SendPerkEmbedAsync(MessageCreateEventArgs e)
        {
            var perkEmbed = new DiscordEmbedBuilder()
                .WithTitle(":star: Minefield Perks :star:")
                .WithColor(DiscordColor.Azure)
                .AddField($":shield: Aegis ({_minefieldService.perkCosts["aegis"]} MF$)", "Your next 5 messages are protected from mines. Does not affect Sacrifice. (20 message cooldown after depletion)", false)
                .AddField($":scroll: Death Pact ({_minefieldService.perkCosts["death_pact"]} MF$ from each user)", "You and the target share message earnings. When you or the target trigger a mine, you both blow up and lose 10 max odds.", false)
                .AddField($":coin: Fortune ({_minefieldService.perkCosts["fortune"]} MF$)", "Double the earnings of your next 5 messages.", false)
                .AddField($":angel: Guardian ({_minefieldService.perkCosts["guardian"]} MF$)", "Negate the effects of the next mine you trigger. In Sacrifice chains, Guardian protects the first user in the chain; otherwise, it triggers last. (15 message cooldown after depletion)", false)
                .AddField($":drop_of_blood: Lifeline ({_minefieldService.perkCosts["lifeline"]} MF$)", "Revive a dead user, resetting their odds and streak. You both receive the earnings of their next 10 messages.", false)
                .AddField($":four_leaf_clover: Luck ({_minefieldService.perkCosts["luck"]} MF$ per point)", "Improve your current odds by a chosen amount. (up to your maximum)", false)
                .AddField($":adhesive_bandage: Restore ({_minefieldService.perkCosts["restore"]} MF$ per point)", "Increase your maximum odds, and current odds, by a chosen amount. (up to a maximum of 100)", false)
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

        private async Task SendUsersEmbedAsync(MessageCreateEventArgs e)
        {
            var usernames = await _userService.GetAllUsernamesAsync(e.Guild.Id);

            var userEmbed = new DiscordEmbedBuilder()
                .WithTitle(":person_standing: Minefield Users :person_standing:")
                .WithColor(DiscordColor.Azure)
                .AddField("__List__", string.Join("\n", usernames), false);

            await e.Message.RespondAsync(embed: userEmbed);
        }
    }

    public class LeaderboardEntry
    {
        public string Name { get; set; } = string.Empty;
        public int Currency { get; set; }
    }
}
