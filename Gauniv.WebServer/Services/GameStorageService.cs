using System.IO.Compression;

namespace Gauniv.WebServer.Services;

/// <summary>
/// Service for handling game file storage on the local filesystem.
/// Games are stored in the "GameUploads" directory within the application.
/// </summary>
public class GameStorageService
{
    private readonly string _storagePath;
    private readonly ILogger<GameStorageService> _logger;
    private const long MaxFileSizeBytes = 50L * 1024 * 1024 * 1024; // 50 GB max

    public GameStorageService(IWebHostEnvironment environment, ILogger<GameStorageService> logger)
    {
        _logger = logger;
        // Store games in a dedicated folder outside wwwroot for security
        _storagePath = Path.Combine(environment.ContentRootPath, "GameUploads");
        
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
            _logger.LogInformation("Created game storage directory at: {Path}", _storagePath);
        }
    }

    /// <summary>
    /// Saves a game file to the storage directory.
    /// </summary>
    /// <param name="gameFile">The uploaded game file (must be a .zip file)</param>
    /// <param name="gameId">The game ID to use for naming the file</param>
    /// <returns>A tuple containing success status, file size in MB, and optional error message</returns>
    public async Task<(bool Success, int SizeInMB, string? ErrorMessage)> SaveGameFileAsync(IFormFile gameFile, int gameId)
    {
        try
        {
            // Validate file extension
            var extension = Path.GetExtension(gameFile.FileName).ToLowerInvariant();
            if (extension != ".zip")
            {
                return (false, 0, "Le fichier doit être au format .zip");
            }

            // Validate file size
            if (gameFile.Length > MaxFileSizeBytes)
            {
                return (false, 0, $"Le fichier est trop volumineux. Taille maximum : {MaxFileSizeBytes / (1024 * 1024 * 1024)} Go");
            }

            if (gameFile.Length == 0)
            {
                return (false, 0, "Le fichier est vide");
            }

            // Validate it's actually a valid ZIP file
            try
            {
                using var stream = gameFile.OpenReadStream();
                using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
                // If we get here, it's a valid ZIP
                stream.Position = 0; // Reset for actual saving
            }
            catch (InvalidDataException)
            {
                return (false, 0, "Le fichier n'est pas un fichier ZIP valide");
            }

            // Generate safe file name
            var fileName = $"game_{gameId}.zip";
            var filePath = Path.Combine(_storagePath, fileName);

            // Delete existing file if it exists
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted existing game file: {Path}", filePath);
            }

            // Save the file using streaming to handle large files
            await using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true))
            {
                await gameFile.CopyToAsync(fileStream);
            }

            // Calculate size in MB (rounded up)
            var sizeInMB = (int)Math.Ceiling(gameFile.Length / (1024.0 * 1024.0));
            
            _logger.LogInformation("Saved game file: {Path}, Size: {Size} MB", filePath, sizeInMB);
            
            return (true, sizeInMB, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving game file for game {GameId}", gameId);
            return (false, 0, $"Erreur lors de l'enregistrement du fichier : {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the path to a game file.
    /// </summary>
    /// <param name="gameId">The game ID</param>
    /// <returns>The file path if it exists, null otherwise</returns>
    public string? GetGameFilePath(int gameId)
    {
        var fileName = $"game_{gameId}.zip";
        var filePath = Path.Combine(_storagePath, fileName);
        return File.Exists(filePath) ? filePath : null;
    }

    /// <summary>
    /// Gets the size of a game file in MB.
    /// </summary>
    /// <param name="gameId">The game ID</param>
    /// <returns>Size in MB or 0 if file doesn't exist</returns>
    public int GetGameFileSizeInMB(int gameId)
    {
        var filePath = GetGameFilePath(gameId);
        if (filePath == null) return 0;
        
        var fileInfo = new FileInfo(filePath);
        return (int)Math.Ceiling(fileInfo.Length / (1024.0 * 1024.0));
    }

    /// <summary>
    /// Checks if a game file exists.
    /// </summary>
    /// <param name="gameId">The game ID</param>
    /// <returns>True if the file exists</returns>
    public bool GameFileExists(int gameId)
    {
        return GetGameFilePath(gameId) != null;
    }

    /// <summary>
    /// Deletes a game file.
    /// </summary>
    /// <param name="gameId">The game ID</param>
    /// <returns>True if deleted successfully</returns>
    public bool DeleteGameFile(int gameId)
    {
        try
        {
            var filePath = GetGameFilePath(gameId);
            if (filePath == null) return true; // Already doesn't exist
            
            File.Delete(filePath);
            _logger.LogInformation("Deleted game file: {Path}", filePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting game file for game {GameId}", gameId);
            return false;
        }
    }

    /// <summary>
    /// Opens a stream to read a game file.
    /// </summary>
    /// <param name="gameId">The game ID</param>
    /// <returns>A FileStream or null if file doesn't exist</returns>
    public FileStream? OpenGameFileStream(int gameId)
    {
        var filePath = GetGameFilePath(gameId);
        if (filePath == null) return null;
        
        return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true);
    }
}
