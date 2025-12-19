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
        private string buyButtonText = "Buy";
        
        [ObservableProperty]
        private bool isLoading = false;

        public IAsyncRelayCommand? BuyOrDownloadCommand { get; private set; }

        [ObservableProperty] 
        private string? gameId;
        
        [ObservableProperty]
        private bool isConnected = NetworkService.Instance.Token != null;

        public GameDetailsViewModel()
        {
            BuyOrDownloadCommand = new AsyncRelayCommand(ExecuteBuyOrDownloadAsync);

        }
        
        private async Task LoadGameDetails()
        {
            if (GameId == null) return;
            if(!Int32.TryParse(GameId, out _)) return;
            
            IsLoading = true;

            var id = Int32.Parse(GameId);
            var gameDetails = await NetworkService.Instance.GamesClient.DetailsAsync(id);
            Game = gameDetails;
            UpdateButtonText();
            IsLoading = false;
        }
        
        partial void OnGameIdChanged(string? value)
        {
            // appelé automatiquement par ObservableProperty lorsque GameId change
            _ = LoadGameDetails();
        }

        partial void OnGameChanged(OwnedGameFullDto? value)
        {
            // appelé automatiquement par ObservableProperty lorsque Game change
            UpdateButtonText();
        }

        private void UpdateButtonText()
        {
            if(Game == null) return;
            BuyButtonText = Game.IsOwned ? "Download" : "Buy";
        }

        private async Task ExecuteBuyOrDownloadAsync()
        {
            if (Game == null) return;
            if (Game.IsOwned)
            {
                // logique de téléchargement (placeholder) : ouvrir un navigateur vers l'URL de téléchargement ou lancer la logique
                // Pour l'instant on simule par un TODO ou log ; vous pouvez remplacer par l'appel approprié
                System.Diagnostics.Debug.WriteLine($"Download requested for game {Game.Id}");
                return;
            }

            try
            {
                // Appel à l'API pour acheter le jeu
                await NetworkService.Instance.GamesClient.BuyAsync(Game.Id);
                // Si l'appel réussit, rafraîchir les détails
                var refreshed = await NetworkService.Instance.GamesClient.DetailsAsync(Game.Id);
                Game = refreshed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Buy failed: {ex}");
            }
        }
    }
}
