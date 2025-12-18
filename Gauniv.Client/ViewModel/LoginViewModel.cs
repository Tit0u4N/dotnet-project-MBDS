using CommunityToolkit.Mvvm.ComponentModel;
using Gauniv.Client.Pages;
using Gauniv.Client.Services;

namespace Gauniv.Client.ViewModel;
using System.ComponentModel;
using System.Windows.Input;
using Index = Gauniv.Client.Pages.Index;

public class LoginViewModel : ObservableObject
{
    private string _username;
    private string _password;

    public string Username
    {
        get => _username;
        set { _username = value; }
    }

    public string Password
    {
        get => _password;
        set { _password = value; }
    }

    public ICommand LoginCommand { get; }

    public LoginViewModel()
    {
        _username = "";
        _password = "";
        LoginCommand = new Command(OnLogin);
    }

    private async void OnLogin()
    {
        if (Username == "admin" && Password == "1234")
        {
            NetworkService.Instance.Token = "fake-jwt";
            NetworkService.Instance.connected();
            NavigationService.Instance.Navigate<Profile>([]);
        }
        else
        {
            await Application.Current.MainPage.DisplayAlert("Erreur", "Identifiants incorrects", "OK");
        }
    }
}