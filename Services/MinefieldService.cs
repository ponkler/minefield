using Minefield.Entities;

namespace Minefield.Services
{
    public class MinefieldService
    {
        private readonly UserService _userService;
        private readonly Random _rng = new Random();

        public readonly Dictionary<string, int> perkCosts = new Dictionary<string, int>
        {
            { "aegis", 60 },
            { "fortune", 50 },
            { "guardian", 75 },
            { "lifeline", 100 },
            { "luck", 80 },
            { "sacrifice", 40 },
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
            bool guardianUsed = false;
            List<(MinefieldUser provider, MinefieldUser target)> sacrifices = new List<(MinefieldUser provider, MinefieldUser target)>();

            if (!user.IsAlive) 
            {
                return new RollResult
                {
                    Odds = -1,
                    Roll = -1,
                    Triggered = false,
                    CloseCall = false,
                    GuardianUsed = false
                };
            }

            user.CurrentStreak++;
            user.TotalMessages++;
            user.MessagesSinceAegis++;
            user.MessagesSinceGuardian++;

            int currencyToAdd = 0;
            if (user.LuckCharges > 0)
            {
                currencyToAdd = user.CurrentStreak * 2;
                user.LuckCharges--;
            }
            else
            {
                currencyToAdd = user.CurrentStreak;
            }
            user.Currency += currencyToAdd;
            user.LifetimeCurrency += currencyToAdd;

            if (user.LifelineProvider != null)
            {
                user.LifelineProvider.LifelineCharges--;
                user.LifelineProvider.Currency += currencyToAdd;

                if (user.LifelineProvider.LifelineCharges == 0)
                {
                    user.LifelineProvider = null;
                }
            }

            if (user.SymbioteProvider != null)
            {
                user.SymbioteProvider.SymbioteCharges--;
                user.SymbioteProvider.Currency += currencyToAdd;

                if (user.SymbioteProvider.SymbioteCharges == 0)
                {
                    user.SymbioteProvider = null;
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
                if (user.HasGuardian)
                {
                    user.HasGuardian = false;
                    guardianUsed = true;
                }
                else
                {
                    sacrifices = await BlowUpAsync(user);
                }
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
                GuardianUsed = guardianUsed,
                Sacrifices = sacrifices
            };
        }

        public async Task<bool> ActivateAegisAsync(MinefieldUser user)
        {
            if (user.Currency < perkCosts["aegis"]) { return false; }

            user.Currency -= perkCosts["aegis"];
            user.AegisCharges = 5;
            user.MessagesSinceAegis = 1;

            await _userService.SaveAsync();
            return true;
        }

        public async Task<bool> ActivateFortuneAsync(MinefieldUser user)
        {
            if (user.Currency < perkCosts["fortune"]) { return false; }

            user.Currency -= perkCosts["fortune"];
            user.CurrentOdds = Math.Min(user.CurrentOdds + 5, 50);

            await _userService.SaveAsync();
            return true;
        }

        public async Task<bool> ActivateGuardianAsync(MinefieldUser user)
        {
            if (user.Currency < perkCosts["guardian"]) { return false; }

            user.Currency -= perkCosts["guardian"];
            user.HasGuardian = true;
            user.MessagesSinceGuardian = 1;

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

            target.IsAlive = true;
            target.CurrentOdds = 50;
            target.CurrentStreak = 0;
            target.LifelineProviderId = user.UserId;
            target.LifelineProviderServerId = user.ServerId;
            target.LifelineProvider = user;

            UserRevived?.Invoke(target);

            await _userService.SaveAsync();
            return true;
        }

        public async Task<bool> ActivateLuckAsync(MinefieldUser user)
        {
            if (user.Currency < perkCosts["luck"]) { return false; }

            user.Currency -= perkCosts["luck"];
            user.LuckCharges = 5;

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
            await RemoveLifelineAsync(user);
            await RemoveSacrificeAsync(user);
            await RemoveSymbioteAsync(user);
        }

        public async Task<List<(MinefieldUser provider, MinefieldUser target)>> BlowUpAsync(MinefieldUser user)
        {
            user.CurrentStreak = 0;

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
    }

    public class RollResult
    {
        public int Odds { get; set; }
        public int Roll { get; set; }
        public bool Triggered { get; set; }
        public bool CloseCall { get; set; }
        public bool GuardianUsed { get; set; }
        public List<(MinefieldUser provider, MinefieldUser target)> Sacrifices { get; set; } = new List<(MinefieldUser provider, MinefieldUser target)>();
    }
}
