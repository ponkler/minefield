using Microsoft.EntityFrameworkCore;
using Minefield.Data;
using Minefield.Entities;

namespace Minefield.Services
{
    public class CofferService
    {
        private readonly MinefieldDbContext _context;

        public CofferService(MinefieldDbContext context)
        {
            _context = context;
        }

        public async Task<Coffer> GetOrCreateCofferAsync(ulong serverId)
        {
            var coffer = await _context.Coffers.FirstOrDefaultAsync(c => c.ServerId == serverId);

            if (coffer == null)
            {
                coffer = new Coffer
                {
                    ServerId = serverId,
                    Amount = 0,
                    Opening = false
                };
                _context.Coffers.Add(coffer);
                await _context.SaveChangesAsync();
            }

            return coffer;
        }

        public async Task<int> GetCofferAmountAsync(ulong serverId)
        {
            return (await _context.Coffers.FirstAsync(c => c.ServerId == serverId)).Amount;
        }

        public async Task<int> GetCofferTicketSaleCountAsync(ulong serverId)
        {
            return await _context.Tickets.Where(t => t.ServerId == serverId)
                .Select(t => t.Count)
                .SumAsync();
        }

        public async Task AddToCofferAmountAsync(ulong serverId, int amount)
        {
            (await _context.Coffers.FirstAsync(c => c.ServerId == serverId)).Amount += amount;
            await _context.SaveChangesAsync();
        }

        public async Task SetCofferAmountAsync(ulong serverId, int amount)
        {
            (await _context.Coffers.FirstAsync(c => c.ServerId == serverId)).Amount = amount;
            await _context.SaveChangesAsync();
        }

        public async Task ToggleCofferOpening(ulong serverId)
        {
            (await _context.Coffers.FirstAsync(c => c.ServerId == serverId)).Opening = 
                !(await _context.Coffers.FirstAsync(c => c.ServerId == serverId)).Opening;
            await _context.SaveChangesAsync();
        }

        public async Task<bool> CheckIfUserHasTicketsAsync(MinefieldUser user)
        {
            return await _context.Tickets
                .AnyAsync(t => t.UserId == user.UserId && t.ServerId == user.ServerId);
        }

        public async Task<List<(MinefieldUser User, int Amount)>> GetUserTicketsAsync(ulong serverId)
        {
            var userTicketsObj = await _context.Tickets.Where(t => t.ServerId == serverId)
                .Select(t => new {t.User, t.Count})
                .ToListAsync();

            List<(MinefieldUser, int)> userTicketsTuple = new List<(MinefieldUser, int)>();

            foreach (var user in userTicketsObj)
            {
                userTicketsTuple.Add((user.User, user.Count));
            }

            return userTicketsTuple;
        }

        public async Task<int> GetUserTicketCountAsync(MinefieldUser user)
        {
            return (await _context.Tickets
                .FirstOrDefaultAsync(t => t.UserId == user.UserId && t.ServerId == user.ServerId))?
                .Count ?? 0;
        }

        public async Task AddUserTicketsAsync(MinefieldUser user, int amount)
        {
            var tickets = await _context.Tickets
                .FirstOrDefaultAsync(t => t.UserId == user.UserId && t.ServerId == user.ServerId);

            if (tickets != null)
            {
                tickets.Count += amount;
                _context.Tickets.Update(tickets);
            }
            else
            {
                await _context.Tickets.AddAsync(new CofferTicket
                {
                    UserId = user.UserId,
                    ServerId = user.ServerId,
                    Count = amount
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task ClearTicketsAsync(ulong serverId)
        {
            _context.Tickets.RemoveRange(await _context.Tickets
                .Where(t => t.ServerId == serverId)
                .ToListAsync()
            );

            await _context.SaveChangesAsync();
        }
    }
}
