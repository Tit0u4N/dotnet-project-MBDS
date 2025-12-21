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

        // Propriétés pour le téléchargement
        [ObservableProperty]
        private bool isDownloading = false;
        
        [ObservableProperty]
        private bool isUnzipping = false;
        // Etat de pause
        [ObservableProperty]
        private bool isPaused = false;
        
        [ObservableProperty]
        private bool isCancelable = false;
        
        [ObservableProperty]
        private bool isUninstalling = false;

        [ObservableProperty]
        private bool isDownloaded = false;

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
            if (!(gameDetails?.IsOwned ?? false))
                _currentState = GameState.NotOwned;
            else if(DownloadService.Instance.IsGameDownloaded(_game.Name))
                _currentState = GameState.Downloaded;
            else
                _currentState = GameState.OwnedNotDownloaded;

            IsLoading = false;
        }
        
        partial void OnGameIdChanged(string? value)
        {
            // appelé automatiquement par ObservableProperty lorsque GameId change
            _ = LoadGameDetails();
        }



        [RelayCommand]
        public async Task Buy()
        {
            if (Game == null) return;
            try
            {
                IsBuyButtonEnabled = false;
                // Appel à l'API pour acheter le jeu
                await NetworkService.Instance.GamesClient.BuyAsync(Game.Id);
                // Si l'appel réussit, rafraîchir les détails
                // ajoute un wait de 10 seconds pour tester
                await Task.Delay(10000);

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

        // Commande de téléchargement
        [RelayCommand]
        public Task Download()
        {
            return StartDownloadAsync(resume: false);
        }

        // Commande de reprise
        [RelayCommand]
        public Task Resume()
        {
            return StartDownloadAsync(resume: true);
        }

        // Commande de pause
        [RelayCommand]
        public void Pause()
        {
            // Délégué vers le service qui annule le token
            DownloadService.Instance.Pause();
            _currentState = GameState.Paused;
        }
        
        
        [RelayCommand]
        public void Cancel()
        {
            // Délégué vers le service qui annule le token
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
                // Mise à jour sur le thread UI est gérée automatiquement par Progress<T>
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
                    // Mise à jour sur le thread UI est gérée automatiquement par Progress<T>
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
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            await Application.Current?.Windows[0]?.Page?.DisplayAlertAsync("Téléchargement", "Téléchargement terminé", "OK")!;
                        });
                    });
                }
                else
                {
                    await Task.Run(async () =>
                    {
                        await DownloadService.Instance.DownloadFileAsync(Game.Id.ToString(), Game.Name, downloadProgress, unzipProgressLooker);

                        // Téléchargement terminé
                        _currentState = GameState.Downloaded;
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            await Application.Current?.Windows[0]?.Page?.DisplayAlertAsync("Téléchargement", "Téléchargement terminé", "OK")!;
                        });
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
                await Application.Current?.Windows[0]?.Page?.DisplayAlertAsync("Erreur", "Le téléchargement a échoué", "OK")!;
            }
            finally
            {
                if(_currentState != GameState.Downloaded && _currentState != GameState.Paused)
                    _currentState = GameState.OwnedNotDownloaded;
            }
        }
        

        private void UpdateGameState(GameState newState)
        {
            IsBuyButtonEnabled = newState == GameState.NotOwned && IsConnected;
            IsNotOwned = newState == GameState.NotOwned;
            IsDownloading = newState == GameState.Downloading;
            IsCancelable = newState is GameState.Downloading or GameState.Paused;
            IsUnzipping = newState == GameState.Unzipping;
            IsPaused = newState == GameState.Paused;
            IsDownloaded = newState == GameState.Downloaded;
            IsDownloadButtonEnabled = newState == GameState.OwnedNotDownloaded;
            IsUninstalling = newState == GameState.Uninstalling;
            IsProgressBarEnabled = newState is GameState.Downloading or GameState.Unzipping or GameState.Uninstalling or GameState.Paused;
        }
    }
    
    internal enum GameState
    {
        NotOwned,
        OwnedNotDownloaded,
        Downloading,
        Downloaded,
        Unzipping,
        Uninstalling,
        Paused
    }
}
