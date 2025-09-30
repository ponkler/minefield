using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Minefield.Entities;
using System.Collections.Generic;

namespace Minefield.Services
{
    public class BotService
    {
        private readonly DiscordClient _client;
        private readonly CommandService _commandService;
        private readonly MinefieldService _minefieldService;
        private readonly UserService _userService;

        private readonly List<string> CloseCallMessages = new List<string>
        {
            "You take a step forward, and a soft click reverberates underfoot. Your breath catches, adrenaline spikes through you as realization sets in. Gently, you lift your foot and step back, holding your breath. There's a tense moment as you wait, muscles coiled, before the ground shifts without a blast. Safe, for now.",
            "As you trek through the dense undergrowth, a metallic glint catches your eye just as your boot hovers over it. Reflexes kick in. You pivot mid-stride and launch yourself sideways, hitting the ground hard. Behind you, a muffled *thump* sends debris scattering into the air, but you're out of reach. Close call.",
            "Your foot presses down on something that feels just slightly wrong. Time slows as you lean back, testing the tension. In a single motion, you shift all your weight to your back foot, keeping your balance steady. With bated breath, you pull your foot free. The ground remains silent and undisturbed. You exhale, unharmed.",
            "Moving through a narrow trail, your boot presses into the earth, and you hear a faint metallic *click.* Instinct takes over, you dive forward, rolling to the side as fast as you can. You feel a warm gust behind you as the mine detonates, scattering dust and grit into the air, but you've cleared the blast.",
            "Spotting something suspicious partially buried, you freeze, carefully retracting your foot and feeling your heartbeat pound. You reach for a rock, throw it at the ground in front of you, and duck. The explosion that follows sends dirt and metal into the air, but you remain intact, unharmed. You rise, relieved."
        };

        public BotService(DiscordClient client, CommandService commandService,
            MinefieldService minefieldService, UserService userService)
        {
            _client = client;
            _commandService = commandService;
            _minefieldService = minefieldService;
            _userService = userService;

            _minefieldService.UserRevived += HandleUserReviveAsync;
            _minefieldService.ArenaStarted += OnArenaStarted;

            _client.MessageCreated += OnMessageCreatedAsync;
            _client.MessageCreated += async (c, e) =>
            {
                if (e.Message.Author.IsBot) return;
                var server = e.Guild;
                var channel = await GetMinefieldChannelAsync(server.Id);
                var member = await server.GetMemberAsync(e.Message.Author.Id);
                if (e.Channel == channel)
                {
                    Console.WriteLine($"Message received from {e.Message.Author.Username}{(member.Roles.Where(r => r.Name == "Minefield Janitor").FirstOrDefault() == null ? "" : ", Janitor")}: {e.Message.Content}");
                }

                await Task.CompletedTask;
            };

            _client.Ready += async (c, a) =>
            {
                int memberCount = 0;
                int serverCount = 0;

                Console.WriteLine($"Bot connected as {_client.CurrentUser.Username}");
                Console.WriteLine($"Populating...");
                foreach (var kv in _client.Guilds)
                {
                    var memberList = (await kv.Value.GetAllMembersAsync()).ToList();
                    serverCount++;

                    memberCount += await PopulateServer(memberList);
                }

                Console.WriteLine($"Found {memberCount:N0} members across {serverCount:N0} servers.");
            };
        }

        private async Task<int> PopulateServer(List<DiscordMember> members)
        {
            int count = 0;

            foreach (var member in members) 
            {
                if (member.IsBot) continue;
                count++;
                await _userService.GetOrCreateUserAsync(member.Id, member.Guild.Id, member.Username);
            }

            return count;
        }

        private async Task OnMessageCreatedAsync(DiscordClient sender, MessageCreateEventArgs e)
        {
            var channel = await GetMinefieldChannelAsync(e.Guild.Id);
            if (e.Author.IsBot || channel == null || e.Message.Channel.Id != channel.Id) { return; }

            if (e.Message.Content.StartsWith("!"))
                await _commandService.HandleCommandAsync(sender, e);
            else
            {
                var user = await _userService.GetOrCreateUserAsync(e.Author.Id, e.Guild.Id, e.Author.Username);
                RollResult result = await _minefieldService.ProcessMessageAsync(user);

                if (result.Roll == 0)
                {
                    return;
                }

                if (result.Triggered)
                {
                    var deadUser = user;
                    foreach ((MinefieldUser provider, MinefieldUser target) sacrifice in result.Sacrifices)
                    {
                        deadUser = sacrifice.provider;
                        var providerName = (await _client.GetUserAsync(sacrifice.provider.UserId)).Username;
                        var targetName = (await _client.GetUserAsync(sacrifice.target.UserId)).Username;
                        await e.Message.Channel.SendMessageAsync($"{providerName} pushes {targetName} out of the way!");
                    }

                    if (deadUser.HasGuardian)
                    {
                        deadUser.HasGuardian = false;
                        await e.Message.RespondAsync(":angel::boom::angel:");
                    }
                    else
                    {
                        deadUser.IsAlive = false;
                        if (deadUser.DeathPactTarget != null)
                        {
                            deadUser.DeathPactTarget.IsAlive = false;
                            await e.Message.RespondAsync($":scroll: {deadUser.DeathPactTarget.Username} has been claimed by their Death Pact with {deadUser.Username} :scroll:");
                            await HandleUserDeathAsync(deadUser.DeathPactTarget);
                        }
                        await HandleUserDeathAsync(deadUser);
                        await e.Message.RespondAsync(":boom:");
                    }
                    return;
                }

                if (result.CloseCall)
                {
                    var rng = new Random();
                    await e.Message.RespondAsync($"{CloseCallMessages[rng.Next(0, CloseCallMessages.Count)]} ({result.Roll}/{result.Odds})");
                }
            }
        }

        private async Task<DiscordChannel?> GetMinefieldChannelAsync(ulong guildId)
        {
            var guild = await _client.GetGuildAsync(guildId);
            var channel = guild.Channels.Values.FirstOrDefault(c => c.Name.Equals("minefield", StringComparison.OrdinalIgnoreCase));
            return channel;
        }

        private async Task HandleUserDeathAsync(MinefieldUser user)
        {
            await _minefieldService.RemoveBoundPerksAsync(user);

            var server = await _client.GetGuildAsync(user.ServerId);
            var channel = await GetMinefieldChannelAsync(user.ServerId);
            var member = await server.GetMemberAsync(user.UserId);

            if (member.IsOwner) { return; }

            if (channel == null) { return; }

            var perms = channel!.PermissionsFor(member);

            if (!perms.HasPermission(Permissions.SendMessages))
            {
                return;
            }

            await channel.AddOverwriteAsync(member, deny: Permissions.SendMessages | Permissions.AccessChannels);
            await _userService.SaveAsync();
        }

        public async Task HandleUserReviveAsync(MinefieldUser user)
        {
            var server = await _client.GetGuildAsync(user.ServerId);
            var channel = await GetMinefieldChannelAsync(user.ServerId);
            var member = await server.GetMemberAsync(user.UserId);

            if (member.IsOwner) { return; }

            if (channel == null) { return; }

            await channel.DeleteOverwriteAsync(member);
            await _userService.SaveAsync();
        }

        private async Task OnArenaStarted(Arena arena)
        {
            var channel = await GetMinefieldChannelAsync(arena.Participants.First().ServerId);
            await _commandService.SendArenaParticipantsEmbedAsync(_client, channel!, arena.Participants, arena.Payout);
            await Task.Delay(3000);

            List<MinefieldUser> winners = new List<MinefieldUser>();

            List<MinefieldUser> survivors = arena.Participants;

            Dictionary<MinefieldUser, int> participantRolls = new Dictionary<MinefieldUser, int>();

            int round = 1;

            while (winners.Count == 0)
            {
                foreach (var user in survivors)
                {
                    participantRolls[user] = _minefieldService.RollArenaRound();
                }

                if (participantRolls.All(pr => pr.Value == 5))
                {
                    winners = participantRolls.Keys.ToList();
                }

                survivors = participantRolls.Where(pr => pr.Value != 5)
                    .Select(pr => pr.Key)
                    .ToList();

                if (survivors.Count == 1)
                {
                    winners.Add(survivors.First());
                }

                await _commandService.SendArenaRoundEmbedAsync(_client, channel!, participantRolls, round);
                round++;
                await Task.Delay(3000);
            }

            await _minefieldService.ResolveArenaAsync(winners);
            await _commandService.SendArenaResolveEmbedAsync(_client, channel!, winners, arena.Payout);
            return;
        }
    }
}
