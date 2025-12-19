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
    }
}
