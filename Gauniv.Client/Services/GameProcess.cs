namespace Gauniv.Client.Services;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public enum ProcessSuccessStartStatus
{
    Success,
    AlreadyRunning,
    ExecutableNotFound,
    FailedToStart
}

public class GameProcessService
{
    public static GameProcessService Instance { get; } = new GameProcessService();
    
    private Dictionary<string, GameProcess> _processes = new Dictionary<string, GameProcess>();
    
    public GameProcess GetProcess(string gameId)
    {
        if (!_processes.ContainsKey(gameId))
        {
            _processes[gameId] = new GameProcess();
        }
        return _processes[gameId];
    }
    
    public void RemoveProcess(int profileId)
    {
        if (_processes.ContainsKey(profileId.ToString()))
        {
            _processes.Remove(profileId.ToString());
        }
    }
}

public class GameProcess
{
    private Process? _process;

    public bool IsRunning => _process != null && !_process.HasExited;
    public int? ProcessId => _process?.Id;

    public ProcessSuccessStartStatus Launch(string gameDirectory)
    {
        if (IsRunning)
            return ProcessSuccessStartStatus.AlreadyRunning;

        var exePath = FindExecutable(gameDirectory);
        if (exePath == null)
            return ProcessSuccessStartStatus.ExecutableNotFound;

        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            WorkingDirectory = Path.GetDirectoryName(exePath),
            UseShellExecute = true
        };

        _process = Process.Start(startInfo);
        return _process != null ? ProcessSuccessStartStatus.Success : ProcessSuccessStartStatus.FailedToStart;
    }

    public void Stop()
    {
        if (!IsRunning)
            return;

        try
        {
            _process!.Kill(true);
            _process.WaitForExit();
        }
        catch
        {
            // log si nécessaire
        }
        finally
        {
            _process = null;
        }
    }
    
    // Attends la fin du processus et retourne le code de sortie si disponible
    public async Task<int?> WaitForExitAsync()
    {
        if (_process == null)
            return null;

        try
        {
            await _process.WaitForExitAsync();
            int code = _process.ExitCode;
            _process = null;
            return code;
        }
        catch
        {
            _process = null;
            return null;
        }
    }
    
    private static string? FindExecutable(string gameDirectory)
    {
        if (!Directory.Exists(gameDirectory))
            return null;

        // Récupère les sous-dossiers
        var subDirectories = Directory.GetDirectories(gameDirectory);
        var filesCount = Directory.GetFiles(gameDirectory).Length;

        string searchDirectory = gameDirectory;

        // S'il n'y a qu'un seul sous-dossier, on cherche dedans
        if (subDirectories.Length == 1 && filesCount == 0)
        {
            searchDirectory = subDirectories[0];
        }

        // Recherche du premier .exe
        var exe = Directory
            .GetFiles(searchDirectory, "*.exe", SearchOption.TopDirectoryOnly)
            .FirstOrDefault();

        return exe;
    }
}