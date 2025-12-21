using CommunityToolkit.Mvvm.ComponentModel;
using Gauniv.Client.Services;

namespace Gauniv.Client.ViewModel;

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
        try
        {
            bool success = await NetworkService.Instance.Login(Username, Password);
            if (success)
            {
                NavigationService.Instance.Navigate<Index>([], true);
            }
            else
            {
                await AlertService.Instance.ShowAlertAsync("Error", "Invalid credentials", "OK");
            }
        }
        catch (Exception)
        {
            await AlertService.Instance.ShowAlertAsync("Error", "An error occurred during login", "OK");
        }
    }
}