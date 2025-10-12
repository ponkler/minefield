namespace Minefield.Entities
{
    public class CofferTicket
    {
        public ulong ServerId { get; set; }
        public ulong UserId { get; set; }
        
        public int Count { get; set; }

        public Coffer Coffer { get; set; } = null!;
        public MinefieldUser User { get; set; } = null!;
    }
}
