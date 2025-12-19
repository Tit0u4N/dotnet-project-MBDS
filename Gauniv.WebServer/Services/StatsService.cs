using Gauniv.WebServer.Data;
using Gauniv.WebServer.Dtos;
using Gauniv.WebServer.Websocket;
using Microsoft.EntityFrameworkCore;

namespace Gauniv.WebServer.Services;

public class StatsService
{
    private readonly ApplicationDbContext _context;
    private static int _maxPlayersOnPlatform = 0;
    private static readonly object _lock = new();

    public StatsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PlatformStatsDto> GetPlatformStatsAsync()
    {
        var totalGames = await _context.Games.CountAsync();

        var gamesPerCategory = await _context.Set<Category>()
            .Include(c => c.GameCategories)
            .Select(c => new CategoryStatsDto
            {
                CategoryId = c.Id,
                CategoryTitle = c.Title,
                GameCount = c.GameCategories.Count
            })
            .OrderByDescending(c => c.GameCount)
            .ToListAsync();

        var userGameCounts = await _context.Set<UserGame>()
            .GroupBy(ug => ug.UserId)
            .Select(g => g.Count())
            .ToListAsync();

        var averageGamesPerAccount = userGameCounts.Count > 0
            ? userGameCounts.Average()
            : 0;

        var averageTimePlayed = await _context.Set<UserGame>()
            .Where(ug => ug.TimePlayedInMinutes > 0)
            .AverageAsync(ug => (double?)ug.TimePlayedInMinutes) ?? 0;

        var currentPlayersOnline = OnlineHub.GetOnlinePlayersCount();

        lock (_lock)
        {
            if (currentPlayersOnline > _maxPlayersOnPlatform)
            {
                _maxPlayersOnPlatform = currentPlayersOnline;
            }
        }

        var maxPlayersPerGame = await _context.Games
            .Where(g => g.MaxPlayersConnectedSimultaneously > 0)
            .OrderByDescending(g => g.MaxPlayersConnectedSimultaneously)
            .Take(10)
            .Select(g => new GameMaxPlayersDto
            {
                GameId = g.Id,
                GameName = g.Name,
                MaxPlayersConnectedSimultaneously = g.MaxPlayersConnectedSimultaneously
            })
            .ToListAsync();

        return new PlatformStatsDto
        {
            TotalGamesAvailable = totalGames,
            GamesPerCategory = gamesPerCategory,
            AverageGamesPerAccount = Math.Round(averageGamesPerAccount, 2),
            AverageTimePlayedPerGameInMinutes = Math.Round(averageTimePlayed, 2),
            CurrentPlayersOnline = currentPlayersOnline,
            MaxPlayersOnPlatform = Math.Max(_maxPlayersOnPlatform, currentPlayersOnline),
            MaxPlayersPerGame = maxPlayersPerGame
        };
    }
}
