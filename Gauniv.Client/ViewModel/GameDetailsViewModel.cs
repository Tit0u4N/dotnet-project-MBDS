using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gauniv.Client.Proxy;
using Gauniv.Client.Services;

namespace Gauniv.Client.ViewModel
{
    
    [QueryProperty(nameof(GameId), "gameId")]
    public partial class GameDetailsViewModel : ObservableObject
    {
        private GameState _currentState
        {
            get;
            // use UpdateGameState at each change of state
            set
            {
                field = value;  UpdateGameState(value); 
            }
        }

        [ObservableProperty]
        private OwnedGameFullDto? game = null;
        
        
        private OwnedGameFullDto? _game
        {
            get;
            set
            {
                field = value; 
                Game = value;
                Categories = string.Join(", ", value?.GameCategories?.Select(c => c.Title) ?? []);
            }
        } = null;

        [ObservableProperty] 
        private string categories = "";
        
        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        private bool isBuyButtonEnabled = true;
        
        [ObservableProperty]
        private bool isProgressBarEnabled = false;

        [ObservableProperty]
        private bool isNotOwned = true;
        
        [ObservableProperty] 
        private string? gameId;
        
        [ObservableProperty]
        private bool isConnected = NetworkService.Instance.Token != null;

        // Properties related to downloading
        [ObservableProperty]
        private bool isDownloading = false;
        
        [ObservableProperty]
        private bool isUnzipping = false;
        // Pause state
        [ObservableProperty]
        private bool isPaused = false;
        
        [ObservableProperty]
        private bool isCancelable = false;
        
        [ObservableProperty]
        private bool isUninstalling = false;

        [ObservableProperty]
        private bool isDownloaded = false;

        [ObservableProperty]
        private bool isRunning = false;

        [ObservableProperty]
        private double progress = 0.0;
        
        [ObservableProperty]
        private string downloadPercent = "0.0";
        
        [ObservableProperty]
        private string unzipPercent = "0.0";
        
        [ObservableProperty]
        private string deletePercent = "0.0";


        [ObservableProperty]
        private bool isDownloadButtonEnabled = true;

        // Local GameProcess instance to start/stop the game process
        private GameProcess? _gameProcess;
        private bool isEventHandlerAttached = false;

        public GameDetailsViewModel()
        {

        }
        
        private async Task LoadGameDetails()
        {
            if (GameId == null) return;
            if(!Int32.TryParse(GameId, out _)) return;
            
            IsLoading = true;

            var id = Int32.Parse(GameId);
            var gameDetails = await NetworkService.Instance.GamesClient.DetailsAsync(id);
            _game = gameDetails;
            _gameProcess = GameProcessService.Instance.GetProcess(GameId);
            if (!(gameDetails?.IsOwned ?? false))
                _currentState = GameState.NotOwned;
            else if(_gameProcess.IsRunning)
                _currentState = GameState.IsRunning;
            else if(DownloadService.Instance.IsGameDownloaded(_game.Name))
                _currentState = GameState.Downloaded;
            else
                _currentState = GameState.OwnedNotDownloaded;

            IsLoading = false;
        }
        
        partial void OnGameIdChanged(string? value)
        {
            // called automatically by ObservableProperty when GameId changes
            _ = LoadGameDetails();
        }




        [RelayCommand]
        public async Task Buy()
        {
            if (Game == null) return;
            try
            {
                IsBuyButtonEnabled = false;
                await NetworkService.Instance.GamesClient.BuyAsync(Game.Id);

                var refreshed = await NetworkService.Instance.GamesClient.DetailsAsync(Game.Id);
                _game = refreshed;
                if (refreshed?.IsOwned ?? false)
                {
                    _currentState = GameState.OwnedNotDownloaded;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Buy failed: {ex}");
                _currentState = GameState.NotOwned;
            }
        }

        // Download command
        [RelayCommand]
        public Task Download()
        {
            return StartDownloadAsync(resume: false);
        }

        // Resume command
        [RelayCommand]
        public Task Resume()
        {
            return StartDownloadAsync(resume: true);
        }

        // Pause command
        [RelayCommand]
        public void Pause()
        {
            // Delegated to the service which cancels the token
            DownloadService.Instance.Pause();
            _currentState = GameState.Paused;
        }
        
        
        [RelayCommand]
        public void Cancel()
        {
            // Delegated to the service which cancels the token
            DownloadService.Instance.CancelAndDeleteFile(Game?.Name ?? "");
            _currentState = GameState.OwnedNotDownloaded;
            DownloadPercent = "0.0";
            Progress = 0.0;
            UnzipPercent = "0.0";
        }

        [RelayCommand]
        public void UnInstall()
        {
            if (Game == null) return;

            _currentState = GameState.Uninstalling;
            DeletePercent = "0.0";
            Progress = 0.0;

            var deletedProgress = new Progress<double>(p =>
            {
                // UI thread update is handled automatically by Progress<T>
                Progress = p;
                DeletePercent = (p * 100).ToString("F2");
            });
            Task.Run(async () =>
            {
                await DownloadService.Instance.DeleteDownloadedGameAsync(Game.Name, deletedProgress);
                _currentState = GameState.OwnedNotDownloaded;
            });
        }




        private async Task StartDownloadAsync(bool resume)
        {
            if (Game == null) return;
            if( _currentState != GameState.OwnedNotDownloaded && _currentState != GameState.Paused  ) return;

            try
            {
                _currentState = GameState.Downloading;
                DownloadPercent = "0.0";
                Progress = 0.0;
                UnzipPercent = "0.0";

                var downloadProgress = new Progress<double>(p =>
                {
                    // UI thread update is handled automatically by Progress<T>
                    Progress = p;
                    DownloadPercent = (p * 100).ToString("F2");
                });
                
                var unzipProgressLooker = new Progress<double>(p =>
                {
                    if (p > 0)
                        _currentState = GameState.Unzipping;
                    Progress = p;
                    UnzipPercent = (p * 100).ToString("F2");
                });

                if (resume)
                {
                    await Task.Run(async () =>
                    {
                        await DownloadService.Instance.ResumeAsync(Game.Id.ToString(), Game.Name, downloadProgress, unzipProgressLooker);
                        
                        _currentState = GameState.Downloaded;
                    });
                }
                else
                {
                    await Task.Run(async () =>
                    {
                        await DownloadService.Instance.DownloadFileAsync(Game.Id.ToString(), Game.Name, downloadProgress, unzipProgressLooker);

                        // Download finished
                        _currentState = GameState.Downloaded;
                    });
                }
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("Download paused by user.");
                
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Download failed: {ex}");
                await AlertService.Instance.ShowAlertAsync("Error", "Download failed");
            }
            finally
            {
                if(_currentState != GameState.Downloaded && _currentState != GameState.Paused)
                    _currentState = GameState.OwnedNotDownloaded;
            }
        }
        

        [RelayCommand]
        public async Task Start()
        {
            if (Game == null) return;
            if (_gameProcess == null)
            {
                await AlertService.Instance.ShowAlertAsync("Error", "Game process not initialized.");
                return;
            }

            var gameFolder = DownloadService.Instance.GetGameFolderPath(Game.Name);
            if (!DownloadService.Instance.IsGameDownloaded(Game.Name))
            {
                await AlertService.Instance.ShowAlertAsync("Error", "Game not installed. Please download it first.");
                return;
            }

            // Check if already running
            if (_gameProcess.IsRunning)
            {
                await AlertService.Instance.ShowAlertAsync("Info", "The game is already running.");
                _currentState = GameState.IsRunning;
                AttachProcessEndEventHandler();
                return;
            }

            // Try to launch
            var status = _gameProcess.Launch(gameFolder);
            switch (status)
            {
                case ProcessSuccessStartStatus.Success:
                    _currentState = GameState.IsRunning;
                    AttachProcessEndEventHandler();
                    break;
                case ProcessSuccessStartStatus.AlreadyRunning:
                    await AlertService.Instance.ShowAlertAsync("Info", "A game is already running.");
                    _currentState = GameState.IsRunning;
                    break;
                case ProcessSuccessStartStatus.ExecutableNotFound:
                    await AlertService.Instance.ShowAlertAsync("Error", "Executable not found in the game's directory.");
                    _currentState = GameState.OwnedNotDownloaded;
                    break;
                case ProcessSuccessStartStatus.FailedToStart:
                default:
                    await AlertService.Instance.ShowAlertAsync("Error", "Unable to start the game.");
                    _currentState = GameState.OwnedNotDownloaded;
                    break;
            }
        }

        [RelayCommand]
        public async Task Stop()
        {
            if (_gameProcess == null) return;
            if (!_gameProcess.IsRunning)
            {
                await AlertService.Instance.ShowAlertAsync("Info", "No active game process.");
                return;
            }

            await Task.Run(() =>
            {
                _gameProcess.Stop();
            });

            _currentState = GameState.Downloaded;
        }
        
        
        private void AttachProcessEndEventHandler()
        {
            if(isEventHandlerAttached)
                return;
            
            isEventHandlerAttached = true;
            _ = Task.Run(async () =>
            {
                if(_gameProcess == null)
                    return;
                var exitCode = await _gameProcess.WaitForExitAsync();
                       

                if (exitCode.HasValue)
                {
                    if (exitCode.Value != 0)
                    {
                        await AlertService.Instance.ShowAlertAsync("Crash", $"The game terminated unexpectedly (code {exitCode}).");
                    }
                }

                // Update state accordingly
                _currentState = GameState.Downloaded;
                isEventHandlerAttached = false;
            });
        }
        
        // Update UI properties based on the current game state
        private void UpdateGameState(GameState newState)
        {
            IsBuyButtonEnabled = newState == GameState.NotOwned && IsConnected;
            IsNotOwned = newState == GameState.NotOwned;
            IsDownloading = newState == GameState.Downloading;
            IsCancelable = newState is GameState.Downloading or GameState.Paused;
            IsUnzipping = newState == GameState.Unzipping;
            IsPaused = newState == GameState.Paused;
            IsDownloaded = newState == GameState.Downloaded;
            IsRunning = newState == GameState.IsRunning;
            IsDownloadButtonEnabled = newState == GameState.OwnedNotDownloaded;
            IsUninstalling = newState == GameState.Uninstalling;
            IsProgressBarEnabled = newState is GameState.Downloading or GameState.Unzipping or GameState.Uninstalling or GameState.Paused;
        }
    }
    
    // Enum representing the various states a game can be in
    internal enum GameState
    {
        NotOwned,
        OwnedNotDownloaded,
        Downloading,
        Downloaded,
        Unzipping,
        Uninstalling,
        IsRunning,
        Paused
    }
}
