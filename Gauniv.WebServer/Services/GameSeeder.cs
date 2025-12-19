using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Gauniv.WebServer.Data;

namespace Gauniv.WebServer.Services
{
    public class GameSeeder
    {
    private readonly ApplicationDbContext _context; // Remplacez par le nom réel de votre DbContext

    public GameSeeder(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task SeedGamesFromCsvAsync(string csvFilePath)
    {
        // Check if file exists
        if (!File.Exists(csvFilePath))
        {
            Console.WriteLine($"Fichier CSV introuvable : {csvFilePath}");
            return;
        }
        
        // Check if insertion is necessary
        int existingGameCount = await _context.Games.CountAsync();
        if (existingGameCount > 1000)
        {
            Console.WriteLine("La base de données contient déjà plus de 1000 jeux. Importation ignorée.");
            return;
        }

        // Configuration of CsvHelper
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = args => args.Header.ToLower(), // Ignore la casse des entêtes
            MissingFieldFound = null, // Ignore les colonnes manquantes si besoin
            HeaderValidated = null
        };

        using (var reader = new StreamReader(csvFilePath))
        using (var csv = new CsvReader(reader, config))
        {
            var records = csv.GetRecords<GameCsvDto>();
            
            var dateFormat = "M/d/yyyy HH:mm";
            
            foreach (var record in records)
            {

                if (!DateTime.TryParseExact(
                        record.DateGlobal,
                        dateFormat,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTime releaseDate))
                {
                    // Fallback if parsing fails
                    releaseDate = DateTime.UtcNow; 
                }
                releaseDate = DateTime.SpecifyKind(releaseDate, DateTimeKind.Utc);
                
                record.Title = record.Title.Trim('\'');
                record.Developer = record.Developer.Trim('\'');
                record.Publisher = record.Publisher.Trim('\'');

                var random = new Random();
                
                // Creation of Game entity
                var newGame = new Game
                {
                    Name = record.Title,
                    Description = $"Game develop by {record.Developer} and published by {record.Publisher}.",
                    Developer = record.Developer,
                    Publisher = record.Publisher,
                    ReleaseDate = releaseDate,
                    ImageUrl = $"https:{record.BoxImage}.jpg",
                    Price = record.IsFree ? 0m : record.BaseAmount,
                    Rating = record.OverallAvgRating,
                    ReviewCount = record.ReviewCount,
                    GameCategories = new List<GameCategory>(),
                    MaxPlayersConnectedSimultaneously = random.Next(0, 1000),
                    SizeInMB = 100 + random.Next(1, 5000) // Random size between 100MB and 5100MB
                };
                
                //Parse categories
                var addedCategories = new HashSet<string>();
                var rawCategories = record.Genres.Trim('[', ']').Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var rawCategory in rawCategories)
                {
                    var categoryTitle = rawCategory.Trim(' ','\'');
                    if (categoryTitle == "" || categoryTitle.ToLower() == "null"|| categoryTitle.ToLower() == " ")
                        continue;
                    if (addedCategories.Contains(categoryTitle))
                        continue;
                    var category = await GetOrCreateCategoryAsync(categoryTitle);
                    newGame.GameCategories.Add(new GameCategory
                    {
                        Category = category
                    });
                    addedCategories.Add(categoryTitle);
                }

                _context.Games.Add(newGame);
            }

            // Save changes to the database
            await _context.SaveChangesAsync();
            Console.WriteLine("Importation terminée.");
        }
    }
    
    
    public async Task<Category> GetOrCreateCategoryAsync(string categoryTitle)
    {
        var category = await _context.Category
            .FirstOrDefaultAsync(c => c.Title.ToLower() == categoryTitle.ToLower());

        if (category == null)
        {
            category = new Category { Title = categoryTitle };
            _context.Category.Add(category);
            await _context.SaveChangesAsync();
        }

        return category;
    }
    
    

    // DTO interne pour matcher exactement les colonnes du CSV
    // "id","developer","publisher","title","genres","dateGlobal","boxImage","isFree","reviewCount","baseAmount","overallAvgRating"
    private class GameCsvDto
    {
        // L'attribut Name permet de mapper si le nom dans le CSV diffère légèrement ou contient des espaces
        [CsvHelper.Configuration.Attributes.Name("id")]
        public int CsvId { get; set; }

        [CsvHelper.Configuration.Attributes.Name("developer")]
        public string Developer { get; set; } = null!;

        [CsvHelper.Configuration.Attributes.Name("publisher")]
        public string Publisher { get; set; } = null!;

        [CsvHelper.Configuration.Attributes.Name("title")]
        public string Title { get; set; } = null!;

        [CsvHelper.Configuration.Attributes.Name("genres")]
        public string Genres { get; set; } = null!; // String brut (ex: "[Action, Adventure]")

        [CsvHelper.Configuration.Attributes.Name("dateGlobal")]
        public string DateGlobal { get; set; } = null!;

        [CsvHelper.Configuration.Attributes.Name("boxImage")]
        public string BoxImage { get; set; } = null!;

        [CsvHelper.Configuration.Attributes.Name("isFree")]
        public bool IsFree { get; set; }

        [CsvHelper.Configuration.Attributes.Name("reviewCount")]
        public int ReviewCount { get; set; }

        [CsvHelper.Configuration.Attributes.Name("baseAmount")]
        public decimal BaseAmount { get; set; }

        [CsvHelper.Configuration.Attributes.Name("overallAvgRating")]
        public double OverallAvgRating { get; set; }
    }
}
}