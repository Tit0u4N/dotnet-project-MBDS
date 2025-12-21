using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gauniv.Client.Proxy;
using Gauniv.Client.Services;

namespace Gauniv.Client.ViewModel
{
    
    [QueryProperty(nameof(GameId), "gameId")]
    public partial class GameDetailsViewModel : ObservableObject
    {
        [ObservableProperty]
        private OwnedGameFullDto? game = null;
        
        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        private bool isBuyButtonEnabled = true;

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

        [ObservableProperty]
        private double downloadProgress = 0.0;
        
        [ObservableProperty]
        private string downloadPercent = "0.0";

        [ObservableProperty]
        private double unzipProgress = 0.0;
        
        [ObservableProperty]
        private string unzipPercent = "0.0";


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
            Game = gameDetails;
            IsNotOwned = !(gameDetails?.IsOwned ?? false);

            // Initialiser l'état du bouton de téléchargement : activé seulement si possédé, connecté et pas en cours de téléchargement
            IsDownloadButtonEnabled = (gameDetails?.IsOwned ?? false) && IsConnected && !IsDownloading;

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
                Game = refreshed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Buy failed: {ex}");
            }
            finally
            {
                IsBuyButtonEnabled = true;
            }
        }

        // Commande de téléchargement
        [RelayCommand]
        public async Task Download()
        {
            if (Game == null) return;

            try
            {
                IsDownloading = true;
                IsDownloadButtonEnabled = false;
                DownloadProgress = 0.0;
                DownloadPercent = "0.0";
                UnzipProgress = 0.0;
                UnzipPercent = "0.0";

                var progress = new Progress<double>(p =>
                {
                    // Mise à jour sur le thread UI est gérée automatiquement par Progress<T>
                    DownloadProgress = p;
                    DownloadPercent = (p * 100).ToString("F2");
                });
                
                var unzipProgressLooker = new Progress<double>(p =>
                {
                    if (p > 0)
                    {
                        IsUnzipping = true;
                        IsDownloading = false;
                    }
                    UnzipProgress = p;
                    UnzipPercent = (p * 100).ToString("F2");
                });

                await Task.Run(async () =>
                {
                    await DownloadService.Instance.DownloadFileAsync(Game.Id.ToString(), Game.Name, progress, unzipProgressLooker);

                    // Téléchargement terminé
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await Application.Current?.Windows[0]?.Page?.DisplayAlertAsync("Téléchargement", "Téléchargement terminé", "OK")!;
                        
                    });
                });
            }
            catch (OperationCanceledException)
            {
                await Application.Current?.Windows[0]?.Page?.DisplayAlertAsync("Téléchargement", "Téléchargement annulé", "OK")!;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Download failed: {ex}");
                await Application.Current?.Windows[0]?.Page?.DisplayAlertAsync("Erreur", "Le téléchargement a échoué", "OK")!;
            }
            finally
            {
                IsDownloading = false;
                // Réévaluer l'état du bouton (le jeu doit être possédé et connecté)
                IsDownloadButtonEnabled = (Game?.IsOwned ?? false) && IsConnected && !IsDownloading;
            }
        }
    }
}
