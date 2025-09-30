using Minefield.Entities;

namespace Minefield.Services
{
    public class MinefieldService
    {
        private readonly UserService _userService;
        private readonly Random _rng = new Random();

        private Arena? Arena;

        public event Func<Arena, Task>? ArenaStarted;

        public readonly Dictionary<string, int> perkCosts = new Dictionary<string, int>
        {
            { "aegis", 100 },
            { "death_pact", 125 },
            { "fortune", 80 },
            { "guardian", 70 },
            { "lifeline", 100 },
            { "luck", 25 },
            { "restore", 200 },
            { "sacrifice", 50 },
            { "symbiote", 90 }
        };

        public event Func<MinefieldUser, Task>? UserRevived;

        public MinefieldService(UserService userService)
        {
            _userService = userService;
        }

        public async Task<RollResult> ProcessMessageAsync(MinefieldUser user)
        {
            int odds = 0;
            int roll = 0;
            bool triggered = false;
            bool aegisUsed = false;
            List<(MinefieldUser provider, MinefieldUser target)> sacrifices = new List<(MinefieldUser provider, MinefieldUser target)>();

            if (!user.IsAlive) 
            {
                return new RollResult
                {
                    Odds = -1,
                    Roll = -1,
                    Triggered = false,
                    CloseCall = false,
                };
            }

            user.CurrentStreak++;
            user.TotalMessages++;
            if (user.AegisCharges == 0) { user.MessagesSinceAegis++; }
            if (user.HasGuardian == false) { user.MessagesSinceGuardian++; }

            int currencyToAdd = 0;
            if (user.FortuneCharges > 0)
            {
                currencyToAdd = user.CurrentStreak * 2;
                user.FortuneCharges--;
            }
            else
            {
                currencyToAdd = user.CurrentStreak;
            }
            user.Currency += currencyToAdd;
            user.LifetimeCurrency += currencyToAdd;

            if (user.DeathPactTarget != null)
            {
                user.DeathPactTarget.Currency += currencyToAdd;
                user.DeathPactTarget.LifetimeCurrency += currencyToAdd;
            }

            if (user.LifelineProvider != null)
            {
                user.LifelineProvider.LifelineCharges--;
                user.LifelineProvider.Currency += currencyToAdd;
                user.LifelineProvider.LifetimeCurrency += currencyToAdd;

                if (user.LifelineProvider.LifelineCharges == 0)
                {
                    await RemoveLifelineAsync(user.LifelineProvider);
                }
            }

            if (user.SymbioteProvider != null)
            {
                user.SymbioteProvider.SymbioteCharges--;
                user.SymbioteProvider.Currency += currencyToAdd;
                user.SymbioteProvider.LifetimeCurrency += currencyToAdd;

                if (user.SymbioteProvider.SymbioteCharges == 0)
                {
                    await RemoveSymbioteAsync(user.SymbioteProvider);
                }
            }

            if (user.AegisCharges > 0)
            {
                user.AegisCharges--;
                aegisUsed = true;
            }

            if (!aegisUsed)
            {
                roll = _rng.Next(1, user.CurrentOdds + 1);
                triggered = roll == user.CurrentOdds;
            }

            if (triggered)
            {
                user.CurrentStreak = user.CurrentStreak / 2;

                if (user.DeathPactTarget != null)
                {
                    user.MaxOdds = Math.Max(user.MaxOdds - 10, 10);
                    user.DeathPactTarget.MaxOdds = Math.Max(user.DeathPactTarget.MaxOdds - 10, 10);

                    user.DeathPactTarget.CurrentOdds = Math.Min(user.DeathPactTarget.CurrentOdds, user.DeathPactTarget.MaxOdds);
                }
                else
                {
                    user.MaxOdds = Math.Max(user.MaxOdds - 5, 10);
                }

                user.CurrentOdds = Math.Min(user.CurrentOdds, user.MaxOdds);
                
                sacrifices = await BlowUpAsync(user);
            }

            odds = user.CurrentOdds;
            if (user.CurrentOdds > 10) { user.CurrentOdds--; }

            await _userService.SaveAsync();
            return new RollResult
            {
                Odds = odds,
                Roll = roll,
                Triggered = triggered,
                CloseCall = odds - roll <= 5 && !triggered,
                Sacrifices = sacrifices
            };
        }

        public async Task<bool> ActivateAegisAsync(MinefieldUser user)
        {
            if (user.Currency < perkCosts["aegis"]) { return false; }

            user.Currency -= perkCosts["aegis"];
            user.AegisCharges = 5;
            user.MessagesSinceAegis = 0;

            await _userService.SaveAsync();
            return true;
        }

        public async Task<bool> ActivateDeathPactAsync(MinefieldUser user, MinefieldUser targetUser)
        {
            user.Currency -= perkCosts["death_pact"];
            targetUser.Currency -= perkCosts["death_pact"];

            user.DeathPactTargetId = targetUser.UserId;
            user.DeathPactTargetServerId = targetUser.ServerId;
            user.DeathPactTarget = targetUser;

            targetUser.DeathPactTargetId = user.UserId;
            targetUser.DeathPactTargetServerId = user.ServerId;
            targetUser.DeathPactTarget = user;

            await _userService.SaveAsync();
            return true;
        }

        public async Task<bool> ActivateFortuneAsync(MinefieldUser user)
        {
            if (user.Currency < perkCosts["fortune"]) { return false; }

            user.Currency -= perkCosts["fortune"];
            user.FortuneCharges = 5;

            await _userService.SaveAsync();
            return true;
        }

        public async Task<bool> ActivateGuardianAsync(MinefieldUser user)
        {
            if (user.Currency < perkCosts["guardian"]) { return false; }

            user.Currency -= perkCosts["guardian"];
            user.HasGuardian = true;
            user.MessagesSinceGuardian = 0;

            await _userService.SaveAsync();
            return true;
        }

        public async Task<bool> ActivateLifelineAsync(MinefieldUser user, MinefieldUser target)
        {
            if (user.Currency < perkCosts["lifeline"]) { return false; }

            user.Currency -= perkCosts["lifeline"];
            user.LifelineTargetId = target.UserId;
            user.LifelineTargetServerId = target.ServerId;
            user.LifelineTarget = target;
            user.LifelineCharges = 10;

            await ReviveUser(target);

            await _userService.SaveAsync();
            return true;
        }

        public async Task<bool> ActivateLuckAsync(MinefieldUser user, int amount)
        {
            amount = Math.Min(amount, user.MaxOdds - user.CurrentOdds);

            if (user.Currency < perkCosts["luck"] * amount) { return false; }

            user.Currency -= perkCosts["luck"] * amount;
            user.CurrentOdds += amount;

            await _userService.SaveAsync();
            return true;
        }

        public async Task<bool> ActivateRestoreAsync(MinefieldUser user, int amount)
        {
            amount = Math.Min(amount, 50 - user.MaxOdds);
            if (user.Currency < perkCosts["restore"] * amount) { return false; }

            user.Currency -= perkCosts["restore"] * amount;
            user.MaxOdds += amount;

            await _userService.SaveAsync();
            return true;
        }

        public bool CanAssignSacrifice(MinefieldUser fromUser, MinefieldUser toUser)
        {
            var visited = new HashSet<ulong>();
            var current = toUser;

            while (current != null) 
            {
                if (current.UserId == fromUser.UserId)
                {
                    return false;
                }

                if (!visited.Add(current.UserId)) 
                {
                    return false;
                }

                current = current.SacrificeTarget;
            }
            return true;
        }

        public async Task<bool> ActivateSacrificeAsync(MinefieldUser user, MinefieldUser target)
        {
            if (user.Currency < perkCosts["sacrifice"]) { return false; }

            user.Currency -= perkCosts["sacrifice"];

            user.SacrificeTargetId = target.UserId;
            user.SacrificeTargetServerId = target.ServerId;
            user.SacrificeTarget = target;

            target.SacrificeProviderId = user.UserId;
            target.SacrificeProviderServerId = user.ServerId;
            target.SacrificeProvider = user;

            await _userService.SaveAsync();
            return true;
        }

        public async Task<bool> ActivateSymbioteAsync(MinefieldUser user, MinefieldUser target)
        {
            if (user.Currency < perkCosts["symbiote"]) { return false; }

            user.Currency -= perkCosts["symbiote"];
            user.SymbioteTargetId = target.UserId;
            user.SymbioteTargetServerId = target.ServerId;
            user.SymbioteTarget = target;
            user.SymbioteCharges = 5;

            target.SymbioteProviderId = user.UserId;
            target.SymbioteProviderServerId = user.ServerId;
            target.SymbioteProvider = user;

            await _userService.SaveAsync();
            return true;
        }

        public async Task RemoveDeathPactAsync(MinefieldUser user)
        {
            if (user.DeathPactTarget == null) { return; }

            user.DeathPactTarget.DeathPactTargetId = null;
            user.DeathPactTarget.DeathPactTargetServerId = null;
            user.DeathPactTarget.DeathPactTarget = null;

            user.DeathPactTargetId = null;
            user.DeathPactTargetServerId = null;
            user.DeathPactTarget = null;

            await _userService.SaveAsync();
        }

        public async Task RemoveLifelineAsync(MinefieldUser user)
        {
            if (user.LifelineTarget == null) { return; }

            user.LifelineTarget.LifelineProviderId = null;
            user.LifelineTarget.LifelineProviderServerId = null;
            user.LifelineTarget.LifelineProvider = null;

            user.LifelineTargetId = null;
            user.LifelineTargetServerId = null;
            user.LifelineTarget = null;

            user.LifelineCharges = 0;

            await _userService.SaveAsync();
        }

        public async Task RemoveSacrificeAsync(MinefieldUser user)
        {
            if (user.SacrificeTarget == null) { return; }

            user.SacrificeTarget.SacrificeProviderId = null;
            user.SacrificeTarget.SacrificeProviderServerId = null;
            user.SacrificeTarget.SacrificeProvider = null;

            user.SacrificeTargetId = null;
            user.SacrificeTargetServerId = null;
            user.SacrificeTarget = null;

            await _userService.SaveAsync();
        }

        public async Task RemoveSymbioteAsync(MinefieldUser user)
        {
            if (user.SymbioteTarget == null) { return; }

            user.SymbioteTarget.SymbioteProviderId = null;
            user.SymbioteTarget.SymbioteProviderServerId = null;
            user.SymbioteTarget.SymbioteProvider = null;

            user.SymbioteTargetId = null;
            user.SymbioteTargetServerId = null;
            user.SymbioteTarget = null;

            await _userService.SaveAsync();
        }

        public async Task RemoveBoundPerksAsync(MinefieldUser user)
        {
            await RemoveDeathPactAsync(user);
            await RemoveLifelineAsync(user);
            await RemoveSacrificeAsync(user);
            await RemoveSymbioteAsync(user);
        }

        public async Task ReviveUser(MinefieldUser user)
        {
            user.IsAlive = true;
            user.CurrentOdds = user.MaxOdds;
            user.CurrentStreak = 0;
            await RemoveBoundPerksAsync(user);

            UserRevived?.Invoke(user);
        }

        public async Task<List<(MinefieldUser provider, MinefieldUser target)>> BlowUpAsync(MinefieldUser user)
        {
            List<(MinefieldUser provider, MinefieldUser target)> sacrifices = new List<(MinefieldUser provider, MinefieldUser target)>();

            var sacrifice = await _userService.GetUserAsync(user.SacrificeProviderId, user.ServerId);

            if (sacrifice == null) 
            {
                user.IsAlive = false;
                await _userService.SaveAsync();
            }

            while (sacrifice != null)
            {
                sacrifices.Add((sacrifice, sacrifice.SacrificeTarget!));

                sacrifice.Currency += (int)(user.Currency * 0.1);

                sacrifice.SacrificeTarget!.SacrificeProviderId = null;
                sacrifice.SacrificeTarget!.SacrificeProviderServerId = null;
                sacrifice.SacrificeTarget!.SacrificeProvider = null;

                sacrifice.SacrificeTargetId = null;
                sacrifice.SacrificeTargetServerId = null;
                sacrifice.SacrificeTarget = null;

                var newSacrifice = await _userService.GetUserAsync(sacrifice.SacrificeProviderId, sacrifice.ServerId);

                sacrifice = newSacrifice;
                await _userService.SaveAsync();
            }
            return sacrifices;
        }

        public async Task AddUserToArenaAsync(MinefieldUser user)
        {
            user.Currency -= Arena!.BuyIn;
            Arena!.Participants.Add(user);
            Console.WriteLine($"{user.Username} added to Arena. Participants: {Arena!.Participants.Count}");
            await _userService.SaveAsync();
        }

        public bool CanJoinArena(MinefieldUser user)
        {
            if (user.Currency >= Arena!.BuyIn)
            {
                return true;
            }
            return false;
        }

        public bool IsInArena(MinefieldUser user)
        {
            if (Arena!.Participants.Contains(user))
            {
                return true;
            }
            return false;
        }

        public bool ArenaActive()
        {
            return Arena != null;
        }

        public async Task<bool> SetUpArenaAsync(int buyIn, MinefieldUser host)
        {
            Arena = new Arena
            {
                BuyIn = buyIn
            };

            await AddUserToArenaAsync(host);

            await Task.Delay(60000);

            if (Arena.Participants.Count == 1) 
            {
                Arena.Participants.First().Currency += Arena.BuyIn;
                await _userService.SaveAsync();
                Arena = null;
                return false;
            }

            Arena.Payout = Arena.BuyIn * Arena.Participants.Count;
            ArenaStarted?.Invoke(Arena);
            return true;
        }

        public async Task ResolveArenaAsync(List<MinefieldUser> winners)
        {
            int split = Arena!.Payout / winners.Count;

            foreach (var winner in winners)
            {
                winner.Currency += split;
                winner.LifetimeCurrency += split;
                await _userService.SaveAsync();
            }

            Arena = null;
        }

        public int RollArenaRound()
        {
            return _rng.Next(1, 6);
        }
    }

    public class RollResult
    {
        public int Odds { get; set; }
        public int Roll { get; set; }
        public bool Triggered { get; set; }
        public bool CloseCall { get; set; }
        public List<(MinefieldUser provider, MinefieldUser target)> Sacrifices { get; set; } = new List<(MinefieldUser provider, MinefieldUser target)>();
    }

    public class Arena
    {
        public int BuyIn { get; set; }
        public int Payout { get; set; }
        public List<MinefieldUser> Participants { get; set; } = new List<MinefieldUser>();
    }
}
