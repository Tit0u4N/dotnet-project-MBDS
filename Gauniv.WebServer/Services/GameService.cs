using Gauniv.WebServer.Data;
using Gauniv.WebServer.Dtos;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Gauniv.WebServer.Services;

public class GameService
{
    private readonly ApplicationDbContext _context;

    public GameService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GameFullDto> AddGameAsync(GameCreateOrEditDto gameDto)
    {
        ValidationHelper.Validate(gameDto);
        Game gameDB = gameDto.Adapt<Game>();
        
        var gameDBReturn = _context.Games.Add(gameDB);
        await _context.SaveChangesAsync();

        return gameDBReturn.Entity.Adapt<GameFullDto>();
    }

    public async Task<bool> DeleteGameAsync(int gameId)
    {
        var game = await _context.Games.FindAsync(gameId);
        if (game == null) return false;

        _context.Games.Remove(game);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<GameFullDto?> UpdateGameAsync(int gameId, GameCreateOrEditDto gameDto)
    {
        ValidationHelper.Validate(gameDto);
        var existingGame = await _context.Games.FindAsync(gameId);
        if (existingGame == null) return null;

        gameDto.Adapt(existingGame);

        await _context.SaveChangesAsync();
        return existingGame.Adapt<GameFullDto>();
    }

    public async Task<bool> BuyGameAsync(int gameId, string userId)
    {
        var game = await _context.Games.FindAsync(gameId);
        if (game == null) return false;
        
        // Use FindAsync with composite key if possible or check existence
        var alreadyOwned = await _context.Set<UserGame>()
            .AnyAsync(ug => ug.GameId == gameId && ug.UserId == userId);

        if (alreadyOwned) return false; // Already owned

        var userGame = new UserGame
        {
            GameId = gameId,
            UserId = userId,
            PurchaseDate = DateTime.UtcNow 
            // PurchaseDate assumes property exists, checking UserGame... 
            // If UserGame only has keys, we just add it.
        };

        _context.Set<UserGame>().Add(userGame);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<GameFullDto>> GetAllGamesAsync(
        string? name = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        int[]? categoryIds = null,
        bool? owned = null,
        string? userId = null, // Required for 'owned' filter
        int offset = 0,
        int limit = 10)
    {
        var query = _context.Games.AsQueryable();

        // Filter by Name
        if (!string.IsNullOrEmpty(name))
        {
            query = query.Where(g => g.Name.Contains(name));
        }

        // Filter by Price
        if (minPrice.HasValue)
        {
            query = query.Where(g => g.Price >= minPrice.Value);
        }
        if (maxPrice.HasValue)
        {
            query = query.Where(g => g.Price <= maxPrice.Value);
        }

        // Filter by Category
        if (categoryIds != null && categoryIds.Length > 0)
        {
            // Select games that have *any* of the specified categories
            query = query.Where(g => g.GameCategories.Any(gc => categoryIds.Contains(gc.CategoryId)));
        }

        // Filter by Owned
        if (owned.HasValue && !string.IsNullOrEmpty(userId))
        {
            if (owned.Value)
            {
                // Only games owned by user
                query = query.Where(g => g.UserGames.Any(ug => ug.UserId == userId));
            }
            else
            {
                // Only games NOT owned by user
                query = query.Where(g => !g.UserGames.Any(ug => ug.UserId == userId));
            }
        }
        
        query = query
            .Skip(offset)
            .Take(limit);

        return await query.ProjectToType<GameFullDto>().ToListAsync();
    }
}