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
        
        // Ensure ReleaseDate is UTC for PostgreSQL
        if (gameDB.ReleaseDate.Kind == DateTimeKind.Unspecified)
        {
            gameDB.ReleaseDate = DateTime.SpecifyKind(gameDB.ReleaseDate, DateTimeKind.Utc);
        }

        if (gameDto.Categories != null)
        {
            foreach (var categoryName in gameDto.Categories)
            {
                var category = await _context.Set<Category>().FirstOrDefaultAsync(c => c.Title == categoryName);
                if (category != null)
                {
                    gameDB.GameCategories.Add(new GameCategory { Category = category });
                }
            }
        }
        
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
        var existingGame = await _context.Games
            .Include(g => g.GameCategories)
            .ThenInclude(gc => gc.Category)
            .FirstOrDefaultAsync(g => g.Id == gameId);
            
        if (existingGame == null) return null;

        gameDto.Adapt(existingGame);
        
        // Ensure ReleaseDate is UTC for PostgreSQL
        if (existingGame.ReleaseDate.Kind == DateTimeKind.Unspecified)
        {
            existingGame.ReleaseDate = DateTime.SpecifyKind(existingGame.ReleaseDate, DateTimeKind.Utc);
        }

        // Update Categories
        if (gameDto.Categories != null)
        {
            // Remove existing categories that are not in the new list (or just clear all?)
            // Clearing all and re-adding is safer/easier for full replacement
            existingGame.GameCategories.Clear();

            foreach (var categoryName in gameDto.Categories)
            {
                var category = await _context.Set<Category>().FirstOrDefaultAsync(c => c.Title == categoryName);
                if (category != null)
                {
                    existingGame.GameCategories.Add(new GameCategory { Category = category });
                }
            }
        }

        await _context.SaveChangesAsync();
        return existingGame.Adapt<GameFullDto>();
    }

    public async Task<GameFullDto?> GetGameByIdAsync(int gameId)
    {  
        var game = await _context.Games
            .AsNoTracking()
            .Include(g => g.GameCategories)
            .ThenInclude(gc => gc.Category)
            .FirstOrDefaultAsync(g => g.Id == gameId);
        var g = game?.Adapt<Game, GameFullDto>();
        return g;
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

    public async Task<(List<GameFullDto> Games, int TotalCount)> GetAllGamesAsync(
        string? name = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        int[]? categoryIds = null,
        bool? owned = null,
        string? userId = null, // Required for 'owned' filter
        int offset = 0,
        int limit = 10)
    {
        var query = _context.Games
            .AsNoTracking()
            .Include(g => g.GameCategories)
            .ThenInclude(gc => gc.Category)
            .AsQueryable();

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
        
        var totalCount = await query.CountAsync();

        query = query
            .OrderBy(g => g.Id)
            .Skip(offset)
            .Take(limit);

        var games = await query.ProjectToType<GameFullDto>().ToListAsync();
        return (games, totalCount);
    }
}