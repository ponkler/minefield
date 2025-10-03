using DSharpPlus.CommandsNext;
using Minefield.Entities;

namespace Minefield.Services
{
    public class MinefieldService
    {
        private readonly UserService _userService;
        private readonly Random _rng = new Random();

        private Arena? Arena;

        public event Func<CommandContext, Arena, Task>? ArenaStarted;

        public readonly Dictionary<string, int> perkCosts = new Dictionary<string, int>
        {
            { "aegis", 90 },
            { "death_pact", 100 },
            { "fortune", 80 },
            { "guardian", 60 },
            { "lifeline", 70 },
            { "luck", 25 },
            { "restore", 175 },
            { "sacrifice", 75 },
            { "symbiote", 85 }
        };

        public readonly Dictionary<string, int> perkCooldowns = new Dictionary<string, int>
        {
            { "aegis", 20 },
            { "guardian", 15 }
        };

        public event Func<MinefieldUser, Task>? UserRevived;

        public MinefieldService(UserService userService)
        {
            _userService = userService;
        }

        public async Task<RollResult> ProcessMessageAsync(MinefieldUser user)
        {
            if (!user.IsAlive)
            {
                return RollResult.Dead;
            }

            user.CurrentStreak++;
            user.TotalMessages++;

            if (user.AegisCharges == 0) { user.MessagesSinceAegis++; }
            if (!user.HasGuardian) { user.MessagesSinceGuardian++; }

            int currencyToAdd = CalculateEarnings(user);
            await GiveCurrencyAsync(user, currencyToAdd);

            bool aegisUsed = TryUseAegis(user);

            int roll = 0;
            bool triggered = false;

            if (!aegisUsed)
            {
                roll = _rng.Next(1, user.CurrentOdds + 1);
                triggered = roll == user.CurrentOdds;
            }

            List<(MinefieldUser provider, MinefieldUser target)> sacrifices = new();

            if (triggered)
            {
                sacrifices = await GetUserSacrificeChain(user);
            }

            int odds = user.CurrentOdds;
            if (odds > 2) { user.CurrentOdds--; }

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

        private int CalculateEarnings(MinefieldUser user)
        {
            if (user.FortuneCharges > 0)
            {
                user.FortuneCharges--;
                return user.CurrentStreak * 2;
            }
            return user.CurrentStreak;
        }

        private async Task GiveCurrencyAsync(MinefieldUser user, int amount)
        {
            user.Currency += amount;
            user.LifetimeCurrency += amount;

            GiveDeathPactCurrency(user.DeathPactTarget, amount);
            await GiveLifelineCurrency(user.LifelineProvider, user, amount);
            await GiveSymbioteCurrencyAsync(user.SymbioteProvider, user, amount);
        }

        private void GiveDeathPactCurrency(MinefieldUser? target, int amount)
        {
            if (target is null) return;

            target.Currency += amount;
            target.LifetimeCurrency += amount;
        }

        private async Task GiveLifelineCurrency(MinefieldUser? provider, MinefieldUser user, int amount)
        {
            if (provider is null) return;

            provider.Currency += amount;
            provider.LifetimeCurrency += amount;

            provider.LifelineCharges--;

            if (provider.LifelineCharges == 0)
            {
                await RemoveLifelineAsync(provider, user);
            }
        }

        private async Task GiveSymbioteCurrencyAsync(MinefieldUser? provider, MinefieldUser user, int amount)
        {
            if (provider is null) return;

            provider.Currency += amount;
            provider.LifetimeCurrency += amount;

            provider.SymbioteCharges--;

            if (provider.SymbioteCharges == 0)
            {
                await RemoveSymbioteAsync(provider, user);
            }   
        }

        private bool TryUseAegis(MinefieldUser user)
        {
            if (user.AegisCharges > 0)
            {
                user.AegisCharges--;
                return true;
            }
            return false;
        }

        public void HandleOddsDeduction(MinefieldUser user)
        {
            if (user.DeathPactTarget is not null)
            {
                user.MaxOdds = Math.Max(user.MaxOdds - 5, 2);
                user.DeathPactTarget.MaxOdds = Math.Max(user.DeathPactTarget.MaxOdds - 5, 2);

                user.DeathPactTarget.CurrentOdds = Math.Min(
                    user.DeathPactTarget.CurrentOdds,
                    user.DeathPactTarget.MaxOdds
                );
            }
            else
            {
                user.MaxOdds = Math.Max(user.MaxOdds - 2, 2);
            }

            user.CurrentOdds = Math.Min(user.CurrentOdds, user.MaxOdds);
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

            target.LifelineProviderId = user.UserId;
            target.LifelineProviderServerId = user.ServerId;
            target.LifelineProvider = user;

            await _userService.SaveAsync();

            ReviveUser(target);

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
            user.CurrentOdds += amount;

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

        public async Task RemoveDeathPactAsync(MinefieldUser? a, MinefieldUser? b)
        {
            if (a == null || b == null) { return; }

            if (a.DeathPactTarget == null) { return; }
            if (b.DeathPactTarget == null) { return; }

            a.DeathPactTargetId = null;
            a.DeathPactTargetServerId = null;
            a.DeathPactTarget = null;

            b.DeathPactTargetId = null;
            b.DeathPactTargetServerId = null;
            b.DeathPactTarget = null;

            await _userService.SaveAsync();
        }

        public async Task RemoveLifelineAsync(MinefieldUser? provider, MinefieldUser? target)
        {
            if (provider == null) { return; }
            if (target == null) { return; }
            if (provider.LifelineTarget == null) { return; }
            if (target.LifelineProvider == null) { return; }

            target.LifelineProviderId = null;
            target.LifelineProviderServerId = null;
            target.LifelineProvider = null;

            provider.LifelineTargetId = null;
            provider.LifelineTargetServerId = null;
            provider.LifelineTarget = null;

            provider.LifelineCharges = 0;

            await _userService.SaveAsync();
        }

        public async Task RemoveSacrificeAsync(MinefieldUser? provider, MinefieldUser? target)
        {
            if (provider == null) { return; }
            if (target == null) { return; }
            if (provider.SacrificeTarget == null) { return; }
            if (target.SacrificeProvider == null) { return; }

            target.SacrificeProviderId = null;
            target.SacrificeProviderServerId = null;
            target.SacrificeProvider = null;

            provider.SacrificeTargetId = null;
            provider.SacrificeTargetServerId = null;
            provider.SacrificeTarget = null;

            await _userService.SaveAsync();
        }

        public async Task RemoveSymbioteAsync(MinefieldUser? provider, MinefieldUser? target)
        {
            if (provider == null) { return; }
            if (target == null) { return; }
            if (provider.SymbioteTarget == null) { return; }
            if (target.SymbioteProvider == null) { return; }

            target.SymbioteProviderId = null;
            target.SymbioteProviderServerId = null;
            target.SymbioteProvider = null;

            provider.SymbioteTargetId = null;
            provider.SymbioteTargetServerId = null;
            provider.SymbioteTarget = null;

            provider.SymbioteCharges = 0;

            await _userService.SaveAsync();
        }

        public async Task RemoveAllUserRelevantPerks(MinefieldUser? user)
        {
            if (user == null) { return; }

            if (user.AegisCharges > 0) {  user.AegisCharges = 0; }

            if (user.DeathPactTarget != null) { await RemoveDeathPactAsync(user, user.DeathPactTarget); }

            if (user.FortuneCharges > 0) { user.FortuneCharges = 0; }

            if (user.HasGuardian) {  user.HasGuardian = false; }

            if (user.LifelineTarget != null) { await RemoveLifelineAsync(user, user.LifelineTarget); }
            if (user.LifelineProvider != null) { await RemoveLifelineAsync(user.LifelineProvider, user); }

            if (user.SymbioteTarget != null) { await RemoveSymbioteAsync(user, user.SymbioteTarget); }
            if (user.SymbioteProvider != null) { await RemoveSymbioteAsync(user.SymbioteProvider, user); }
            
            await _userService.SaveAsync();
        }

        public void ReviveUser(MinefieldUser user)
        {
            user.IsAlive = true;
            user.CurrentOdds = user.MaxOdds;
            user.CurrentStreak = 0;

            UserRevived?.Invoke(user);
        }

        public async Task<List<(MinefieldUser provider, MinefieldUser target)>> GetUserSacrificeChain(MinefieldUser user)
        {
            List<(MinefieldUser provider, MinefieldUser target)> sacrifices = new List<(MinefieldUser provider, MinefieldUser target)>();

            var sacrifice = await _userService.GetUserAsync(user.SacrificeProviderId, user.ServerId);

            while (sacrifice != null)
            {
                sacrifices.Add((sacrifice, sacrifice.SacrificeTarget!));

                sacrifice.Currency += user.Currency / 5;

                var newSacrifice = await _userService.GetUserAsync(sacrifice.SacrificeProviderId, sacrifice.ServerId);

                if (newSacrifice != null)
                {
                    sacrifice.SacrificeTarget!.SacrificeProviderId = null;
                    sacrifice.SacrificeTarget!.SacrificeProviderServerId = null;
                    sacrifice.SacrificeTarget!.SacrificeProvider = null;

                    sacrifice.SacrificeTargetId = null;
                    sacrifice.SacrificeTargetServerId = null;
                    sacrifice.SacrificeTarget = null;
                }

                sacrifice = newSacrifice;
                await _userService.SaveAsync();
            }
            return sacrifices;
        }

        public async Task AddUserToArenaAsync(MinefieldUser user)
        {
            user.Currency -= Arena!.BuyIn;
            Arena!.Participants.Add(user);
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

        public async Task<bool> SetUpArenaAsync(CommandContext ctx, int buyIn, MinefieldUser host)
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
            ArenaStarted?.Invoke(ctx, Arena);
            return true;
        }

        public async Task ResolveArenaAsync(List<MinefieldUser> winners)
        {
            int split = Arena!.Payout / winners.Count;

            foreach (var winner in winners)
            {
                winner.Currency += split;
                winner.LifetimeCurrency += split - Arena!.BuyIn;
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
        public static RollResult Dead = new RollResult
            {
                Odds = -1,
                Roll = -1,
                Triggered = false,
                CloseCall = false,
            };
}

    public class Arena
    {
        public int BuyIn { get; set; }
        public int Payout { get; set; }
        public List<MinefieldUser> Participants { get; set; } = new List<MinefieldUser>();
    }
}
