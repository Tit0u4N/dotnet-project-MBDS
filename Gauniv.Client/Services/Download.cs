namespace Gauniv.Client.Services;

using System.Net.Http.Headers;
using System.IO.Compression;

public class DownloadService
{
    private const string RelativeUrl = "game/{id}/download";
    private const string GamesPathKey = "games_download_path";
    private const string DefaultGamesPath = "./games";
    private const int BufferSize = 81920; // 80 KB
    private const long ReportIntervalMs = 100;
    
    
    private long _lastReportTime;
    
    public static DownloadService Instance { get; } = new DownloadService();
    
    private readonly HttpClient _httpClient;

    private CancellationTokenSource? _cts;
    
    // Track the current download task so cancellation + delete can wait for disposal
    private Task? _currentDownloadTask;
    private readonly object _deletionLock = new object();
    
    private string _baseUrl = null!;
    
    public string BaseUrl
    {
        get { return _baseUrl; }
        set
        {
            _baseUrl = value;
            if (!string.IsNullOrEmpty(_baseUrl) && !_baseUrl.EndsWith("/"))
                _baseUrl += '/';
        }
    }

    private DownloadService()
    {
        _httpClient = new HttpClient();
        BaseUrl = "http://localhost:5231/";
    }
    
    private void AddAuthorizationTokenToRequest(HttpRequestMessage request)
    {
        var token = NetworkService.Instance.Token;
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
    }
    
    public string GetDownloadPathFromPreferences()
    {
        var path = Preferences.Get(GamesPathKey, string.Empty);
        if (string.IsNullOrWhiteSpace(path))
        {
            // utiliser le dossier local relatif
            path = Path.GetFullPath(DefaultGamesPath);
            Directory.CreateDirectory(path);
            SetDownloadPathToPreferences(path);
            return path;
        }

        try
        {
            Directory.CreateDirectory(path);
        }
        catch
        {
            // ignore
        }
        return path;
    }
    
    public void SetDownloadPathToPreferences(string path)
    {
        Preferences.Set(GamesPathKey, path);
    }
    
    private string FormatGameFileName(string gameName)
    {
        return  gameName.Replace("\"", "").Replace("/", "-").Replace("\\", "-").Replace(" ", "_").Replace(":", "");
    }
    
    public async Task DownloadFileAsync(
        string gameId,
        string gameName,
        IProgress<double>? progress = null,
        IProgress<double>? unzipProgress = null)
    {
        gameName = FormatGameFileName(gameName);
        string url = $"{_baseUrl}{RelativeUrl.Replace("{id}", gameId)}";
        string destinationPath = Path.Combine(GetDownloadPathFromPreferences(), $"{gameName}.zip");

        // Track the active download task so other methods can observe it
        var task = DownloadAsync(url, destinationPath, progress, unzipProgress);
        _currentDownloadTask = task;
        try
        {
            await task;
        }
        finally
        {
            // clear after completion
            if (ReferenceEquals(_currentDownloadTask, task))
                _currentDownloadTask = null;
        }
    }
    
    
    public bool IsGameDownloaded(string gameName)
    {
        gameName = FormatGameFileName(gameName);
        string gameFolderPath = Path.Combine(GetDownloadPathFromPreferences(), gameName);
        return Directory.Exists(gameFolderPath);
    }
    

    private async Task DownloadAsync(
        string url,
        string destinationPath,
        IProgress<double>? progress = null,
        IProgress<double>? unzipProgress = null)
    {
        _cts = new CancellationTokenSource();

        long existingFileSize = GetExistingFileSize(destinationPath);

        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        if (existingFileSize > 0)
        {
            request.Headers.Range =
                new RangeHeaderValue(existingFileSize, null);
        }
        
        AddAuthorizationTokenToRequest(request);

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            _cts.Token);

        response.EnsureSuccessStatusCode();

        long totalBytes = existingFileSize +
            response.Content.Headers.ContentLength.GetValueOrDefault();

        using var httpStream = await response.Content
            .ReadAsStreamAsync(_cts.Token);

        using var fileStream = new FileStream(
            destinationPath,
            FileMode.Append,
            FileAccess.Write,
            FileShare.None,
            BufferSize,
            useAsync: true);

        var buffer = new byte[BufferSize];
        long totalRead = existingFileSize;
        int read;

        _lastReportTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        while ((read = await httpStream.ReadAsync(buffer, _cts.Token)) > 0)
        {
            await fileStream.WriteAsync(
                buffer.AsMemory(0, read),
                _cts.Token);

            totalRead += read;

            if(_lastReportTime + ReportIntervalMs > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                continue;
            _lastReportTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            progress?.Report((double)totalRead / totalBytes);
        }
        
        await fileStream.FlushAsync(_cts.Token);
        fileStream.Close();

        try
        {
            await ExtractZipAsync(destinationPath, unzipProgress, _cts.Token);
            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }

        }
        catch (OperationCanceledException)
        {
            // ignore
        }
    }

    
    public void Pause()
    {
        _cts?.Cancel();
    }
    
    public Task ResumeAsync(
        string gameId,
        string gameName,
        IProgress<double>? progress = null,
        IProgress<double>? unzipProgress = null)
    {
        gameName = FormatGameFileName(gameName);
        string url = $"{BaseUrl}{RelativeUrl.Replace("{id}", gameId)}";
        string destinationPath = Path.Combine(GetDownloadPathFromPreferences(), $"{gameName}.zip");

        var task = DownloadAsync(url, destinationPath, progress, unzipProgress);
        _currentDownloadTask = task;
        // do not await here, caller can await ResumeAsync if desired
        // clear when finished
        task.ContinueWith(t => { if (ReferenceEquals(_currentDownloadTask, task)) _currentDownloadTask = null; }, TaskScheduler.Default);
        return task;
    }

    
    public void CancelAndDeleteFile(string gameName)
    {
        gameName = FormatGameFileName(gameName);
        string destinationPath = Path.Combine(GetDownloadPathFromPreferences(), $"{gameName}.zip");
        
        Pause();

        // Try immediate deletion first
        try
        {
            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }
            return;
        }
        catch (IOException)
        {
            // The file is probably still in use by the download task; we will retry in background
        }

        // Start a background retry to delete the file when it's released.
        // This avoids blocking the UI and avoids throwing an IOException to callers.
        lock (_deletionLock)
        {
            _ = Task.Run(async () =>
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                const int maxMs = 5000;
                const int delayMs = 150;
                while (sw.ElapsedMilliseconds < maxMs)
                {
                    try
                    {
                        if (File.Exists(destinationPath))
                        {
                            File.Delete(destinationPath);
                        }
                        return;
                    }
                    catch (IOException)
                    {
                        // still in use, wait a bit
                    }
                    catch
                    {
                        // other errors: break out
                        return;
                    }

                    // If there is an active download task, prefer to await briefly its completion
                    if (_currentDownloadTask != null)
                    {
                        try
                        {
                            await Task.WhenAny(_currentDownloadTask, Task.Delay(delayMs));
                        }
                        catch
                        {
                            // ignore
                        }
                    }
                    else
                    {
                        await Task.Delay(delayMs);
                    }
                }

                // Last attempt
                try { if (File.Exists(destinationPath)) File.Delete(destinationPath); } catch { /* ignore */ }
            });
        }
    }

    
    // Nouvelle méthode asynchrone avec reporting de progression
    public async Task DeleteDownloadedGameAsync(string gameName, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        gameName = FormatGameFileName(gameName);
        string gameFolderPath = Path.Combine(GetDownloadPathFromPreferences(), gameName);

        if (!Directory.Exists(gameFolderPath))
        {
            progress?.Report(1.0);
            return;
        }

        var files = Directory.EnumerateFiles(gameFolderPath, "*", SearchOption.AllDirectories).ToArray();

        long totalBytes = 0;
        foreach (var f in files)
        {
            try
            {
                var fi = new FileInfo(f);
                totalBytes += fi.Length;
            }
            catch
            {
                // ignore file access errors when calculating size
            }
        }

        if (totalBytes <= 0 && files.Length == 0)
        {
            // nothing to delete (maybe only empty directories)
            try
            {
                Directory.Delete(gameFolderPath, true);
            }
            catch
            {
                // ignore
            }
            progress?.Report(1.0);
            return;
        }

        long deletedBytes = 0;
        int deletedFiles = 0;
        int totalFiles = files.Length;

        // If byte sizes are available, report by bytes; otherwise by file count
        bool useByteProgress = totalBytes > 0;

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long fileLength = 0;
            try
            {
                var fi = new FileInfo(file);
                fileLength = fi.Length;
            }
            catch
            {
                // ignore
            }

            try
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }
            catch
            {
                // ignore deletion errors and continue
            }

            deletedFiles++;
            deletedBytes += fileLength;
            
            if(_lastReportTime + ReportIntervalMs < DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            {
                if (useByteProgress)
                {
                    progress?.Report(Math.Min(1.0, (double)deletedBytes / totalBytes));
                }
                else
                {
                    progress?.Report(Math.Min(1.0, (double)deletedFiles / totalFiles));
                }
                _lastReportTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
            // throttle reporting a bit if caller provides a fast UI
            await Task.Yield();
        }

        // Delete directories from deepest to top
        try
        {
            var directories = Directory.EnumerateDirectories(gameFolderPath, "*", SearchOption.AllDirectories)
                .OrderByDescending(d => d.Length)
                .ToArray();

            foreach (var dir in directories)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try { Directory.Delete(dir, false); } catch { /* ignore */ }
                await Task.Yield();
            }

            // Finally delete the root folder
            try { Directory.Delete(gameFolderPath, false); } catch { /* ignore */ }
        }
        catch
        {
            // ignore unexpected errors while deleting directories
        }

        progress?.Report(1.0);
    }
    
    
    private static long GetExistingFileSize(string path)
    {
        return File.Exists(path)
            ? new FileInfo(path).Length
            : 0;
    }

    private async Task ExtractZipAsync(string zipPath, IProgress<double>? unzipProgress, CancellationToken token)
    {
        if (!File.Exists(zipPath))
            return;

        string extractDir = Path.Combine(
            Path.GetDirectoryName(zipPath) ?? string.Empty,
            Path.GetFileNameWithoutExtension(zipPath));

        Directory.CreateDirectory(extractDir);

        // Ouvrir le fichier zip en tant que stream puis créer un ZipArchive pour le parcourir
        using var zipStream = File.OpenRead(zipPath);
        using var zip = new ZipArchive(zipStream, ZipArchiveMode.Read);

        long totalBytes = zip.Entries
            .Where(e => !string.IsNullOrEmpty(e.Name))
            .Sum(e => e.Length);
        
        if (totalBytes == 0)
        {
            // Fallback: no sizes available or only directories - extract entries using ZipArchive and async copies
            try
            {
                foreach (var entry in zip.Entries)
                {
                    token.ThrowIfCancellationRequested();

                    var entryFullName = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
                    var destinationPath = Path.Combine(extractDir, entryFullName);

                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        // directory
                        Directory.CreateDirectory(destinationPath);
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? extractDir);

                    using var entryStream = entry.Open();
                    using var fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);

                    // Copier de manière asynchrone
                    await entryStream.CopyToAsync(fs, BufferSize, token);
                    await fs.FlushAsync(token);
                }

                unzipProgress?.Report(1.0);
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            return;
        }

        long extracted = 0;
        var buffer = new byte[BufferSize];

        foreach (var entry in zip.Entries)
        {
            token.ThrowIfCancellationRequested();

            var entryFullName = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
            var destinationPath = Path.Combine(extractDir, entryFullName);

            if (string.IsNullOrEmpty(entry.Name))
            {
                // directory
                Directory.CreateDirectory(destinationPath);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? extractDir);

            using var entryStream = entry.Open();
            using var fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);

            int read;
            while ((read = await entryStream.ReadAsync(buffer, token)) > 0)
            {
                await fs.WriteAsync(buffer.AsMemory(0, read), token);
                extracted += read;
                
                if(_lastReportTime + ReportIntervalMs > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                    continue;
                _lastReportTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                unzipProgress?.Report((double)extracted / totalBytes);
            }

            await fs.FlushAsync(token);
        }

        unzipProgress?.Report(1.0);
    }
}
