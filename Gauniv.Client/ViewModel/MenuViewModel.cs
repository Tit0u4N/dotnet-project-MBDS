using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gauniv.Client.Pages;
using Gauniv.Client.Services;

namespace Gauniv.Client.ViewModel
{
    public partial class MenuViewModel : ObservableObject
    {
        [RelayCommand]
        public void GoToProfile() => NavigationService.Instance.Navigate<Profile>([]);

        [ObservableProperty]
        private bool isConnected = NetworkService.Instance.Token != null;

        public MenuViewModel()
        {
            NetworkService.Instance.OnConnectionChange += Instance_OnConnectionChange;
        }

        private void Instance_OnConnectionChange()
        {
            IsConnected = NetworkService.Instance.Token != null;
        }
    }
}
