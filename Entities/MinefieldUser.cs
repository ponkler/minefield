namespace Minefield.Entities
{
    public class MinefieldUser
    {
        public ulong ServerId { get; set; }
        public ulong UserId { get; set; }
        public string Username { get; set; } = null!;

        public int CurrentOdds { get; set; } = 50;
        public int MaxOdds { get; set; } = 50;
        public int CurrentStreak { get; set; } = 0;
        public int Currency { get; set; } = 0;
        public int LifetimeCurrency { get; set; } = 0;
        public int TotalMessages { get; set; } = 0;
        public bool IsAlive { get; set; } = true;
        public bool IsImmune { get; set; } = false;

        public int MessagesSinceCoinFlip { get; set; } = 5;


        // PERKS ACTIVE
        public int AegisCharges { get; set; } = 0;
        public int MessagesSinceAegis { get; set; } = 15;

        public int LifelineCharges { get; set; } = 0;

        public int SymbioteCharges { get; set; } = 0;

        public int FortuneCharges { get; set; } = 0;

        public bool HasGuardian { get; set; } = false;
        public int MessagesSinceGuardian { get; set; } = 15;

        // DEATH PACT CONNECTION
        public ulong? DeathPactTargetId { get; set; }
        public ulong? DeathPactTargetServerId { get; set; }
        public MinefieldUser? DeathPactTarget { get; set; }

        // LIFELINE CONNECTION
        public ulong? LifelineTargetId { get; set; }
        public ulong? LifelineTargetServerId { get; set; }
        public MinefieldUser? LifelineTarget { get; set; }

        public ulong? LifelineProviderId { get; set; }
        public ulong? LifelineProviderServerId { get; set; }
        public MinefieldUser? LifelineProvider { get; set; }

        // SACRIFICE CONNECTION
        public ulong? SacrificeTargetId { get; set; }
        public ulong? SacrificeTargetServerId { get; set; }
        public MinefieldUser? SacrificeTarget { get; set; }

        public ulong? SacrificeProviderId { get; set; }
        public ulong? SacrificeProviderServerId { get; set; }
        public MinefieldUser? SacrificeProvider { get; set; }

        // SYMBIOTE CONNECTION
        public ulong? SymbioteTargetId { get; set; }
        public ulong? SymbioteTargetServerId { get; set; }
        public MinefieldUser? SymbioteTarget { get; set; }

        public ulong? SymbioteProviderId { get; set; }
        public ulong? SymbioteProviderServerId { get; set; }
        public MinefieldUser? SymbioteProvider { get; set; }

        // JOIN PROPERTIES
        public List<CofferTicket> CofferTickets { get; set; } = new List<CofferTicket>();
    }
}
