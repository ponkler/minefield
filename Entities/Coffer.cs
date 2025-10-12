namespace Minefield.Entities
{
    public class Coffer
    {
        public ulong ServerId { get; set; }
        public int Amount { get; set; } = 0;
        public bool Opening = false;

        public List<CofferTicket> Tickets { get; set; } = new List<CofferTicket>();
    }
}
