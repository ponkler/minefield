namespace Minefield.Entities
{
    public class MinefieldUser
    {
        public ulong ServerId { get; set; }
        public ulong UserId { get; set; }

        public int CurrentOdds { get; set; } = 50;
        public int CurrentStreak { get; set; } = 0;
        public int Currency { get; set; } = 0;
        public int LifetimeCurrency { get; set; } = 0;
        public int TotalMessages { get; set; } = 0;
        public bool IsAlive { get; set; } = true;


        // PERKS ACTIVE
        public int AegisCharges { get; set; } = 0;
        public int MessagesSinceAegis { get; set; } = 20;

        public int LifelineCharges { get; set; } = 0;

        public int SymbioteCharges { get; set; } = 0;

        public int LuckCharges { get; set; } = 0;

        public bool HasGuardian { get; set; } = false;
        public int MessagesSinceGuardian { get; set; } = 30;

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
    }
}
