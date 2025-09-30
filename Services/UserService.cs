using Minefield.Data;
using Minefield.Entities;
using Microsoft.EntityFrameworkCore;
using DSharpPlus;

namespace Minefield.Services
{
    public class UserService
    {
        private readonly MinefieldDbContext _context;

        public UserService(MinefieldDbContext context)
        {
            _context = context;
        }

        public async Task<MinefieldUser> GetOrCreateUserAsync(ulong userId, ulong serverId, string username)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId && u.ServerId == serverId);

            if (user == null)
            {
                user = new MinefieldUser
                {
                    UserId = userId,
                    ServerId = serverId,
                    Username = username
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

        public async Task<List<string>> GetAllUsernamesAsync(ulong serverId)
        {
            return await _context.Users.Where(u => u.ServerId == serverId)
                .Select(u => u.Username)
                .ToListAsync();
        }

        public async Task<MinefieldUser?> ResetUserAsync(ulong userId, ulong serverId)
        {
            var user = await GetUserAsync(userId, serverId);
            
            if (user == null) { return null; }
            var name = user.Username;
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            user = await GetOrCreateUserAsync(userId, serverId, name);
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

        public async Task<List<MinefieldUser>> GetLinkedUsers(MinefieldUser sourceUser)
        {
            return await _context.Users
                .Where(u => (u.DeathPactTargetId == sourceUser.UserId || 
                    u.DeathPactTargetId == sourceUser.UserId ||
                    u.LifelineProviderId == sourceUser.UserId || 
                    u.SacrificeProviderId == sourceUser.UserId || 
                    u.SymbioteProviderId == sourceUser.UserId) &&
                    sourceUser.ServerId == u.ServerId)
                .ToListAsync();
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
