using Minefield.Data;
using Minefield.Entities;
using Microsoft.EntityFrameworkCore;

namespace Minefield.Services
{
    public class UserService
    {
        private readonly MinefieldDbContext _context;

        public UserService(MinefieldDbContext context)
        {
            _context = context;
        }

        public async Task<MinefieldUser> GetOrCreateUserAsync(ulong userId, ulong serverId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId && u.ServerId == serverId);

            if (user == null)
            {
                user = new MinefieldUser
                {
                    UserId = userId,
                    ServerId = serverId
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            return user;
        }

        public async Task<MinefieldUser?> GetUserAsync(ulong? userId, ulong serverId)
        {
            if (userId == null) { return null; }

            return await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId && u.ServerId == serverId);
        }

        public async Task<MinefieldUser?> ResetUserAsync(ulong userId, ulong serverId)
        {
            var user = await GetUserAsync(userId, serverId);

            if (user == null) { return null; }
            _context.Users.Remove(user);

            user = await GetOrCreateUserAsync(userId, serverId);
            return user;
        }

        public async Task<List<MinefieldUser>> GetLeaderboardAsync(ulong serverId)
        {
            var leaderboard = await _context.Users.Where(u => u.ServerId == serverId)
                .OrderByDescending(u => u.Currency)
                .Take(10)
                .ToListAsync();

            return leaderboard;
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
