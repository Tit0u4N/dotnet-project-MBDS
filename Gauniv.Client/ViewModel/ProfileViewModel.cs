using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gauniv.Client.Pages;
using Gauniv.Client.Proxy;
using Gauniv.Client.Services;

namespace Gauniv.Client.ViewModel
{

    internal partial class ProfileViewModel : ObservableObject
    {
        
        [ObservableProperty]
        private bool isLoading;
        
        [ObservableProperty]
        private bool isToastVisible;
        
        [ObservableProperty]
        private string toastMessage = null!;

        [ObservableProperty]
        private string email = null!;

        [ObservableProperty] 
        private string passwordForEmailChange = null!;

        [ObservableProperty]
        private string currentPassword = null!;

        [ObservableProperty]
        private string newPassword = null!;

        [ObservableProperty]
        private string downloadPath;

        public IAsyncRelayCommand ChangeEmailCommand { get; }
        public IAsyncRelayCommand ChangePasswordCommand { get; }
        public IAsyncRelayCommand SetDownloadPathCommand { get; }
        public IAsyncRelayCommand LogoutCommand { get; }

        public ProfileViewModel()
        {
            DownloadPath = DownloadService.Instance.GetDownloadPathFromPreferences();

            ChangeEmailCommand = new AsyncRelayCommand(ChangeEmailAsync);
            ChangePasswordCommand = new AsyncRelayCommand(ChangePasswordAsync);
            SetDownloadPathCommand = new AsyncRelayCommand(SetDownloadPathAsync);
            LogoutCommand = new AsyncRelayCommand(LogoutAsync);
        }

        private async Task ChangeEmailAsync()
        {
            if (string.IsNullOrWhiteSpace(Email))
                return;
            
            IsLoading = true;
            try
            {
                var dto = new ChangeEmailDto { NewEmail = Email , CurrentPassword = PasswordForEmailChange };
                await NetworkService.Instance.UserClient.ChangeEmailAsync(dto);
                ToastMessage = "Email changed successfully.";
                IsLoading = false;
                IsToastVisible = true;
                await Task.Delay(3000);
                IsToastVisible = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Change email failed: {ex.Message}");
                await AlertService.Instance.ShowAlertAsync("Error", $"Change email failed: {ex.Message}", "OK")!;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ChangePasswordAsync()
        {
            if (string.IsNullOrWhiteSpace(CurrentPassword) || string.IsNullOrWhiteSpace(NewPassword))
                return;

            IsLoading = true;
            try
            {
                var dto = new ChangePasswordDto { CurrentPassword = CurrentPassword, NewPassword = NewPassword };
                await NetworkService.Instance.UserClient.ChangePasswordAsync(dto);
                ToastMessage = "Password changed successfully.";
                IsLoading = false;
                IsToastVisible = true;
                await Task.Delay(3000);
                IsToastVisible = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Change password failed: {ex.Message}");
                await AlertService.Instance.ShowAlertAsync("Error", $"Change password failed: {ex.Message}", "OK")!;
            } 
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SetDownloadPathAsync()
        {
            try
            {
                var folder = await FolderPicker.Default.PickAsync();
                if (folder.Folder != null)
                {
                    DownloadPath = folder.Folder.Path;
                    DownloadService.Instance.SetDownloadPathToPreferences(DownloadPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Save download path failed: {ex.Message}");
            }

            await Task.CompletedTask;
        }

        private async Task LogoutAsync()
        {
            IsLoading = true;
            try
            {
                var success = await NetworkService.Instance.Logout();
                if (!success)
                {
                    throw new Exception("Logout failed on server.");
                }
                NavigationService.Instance.Navigate<LoginPage>(new Dictionary<string, object>(), true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logout failed: {ex.Message}");
                await AlertService.Instance.ShowAlertAsync("Error", $"Logout failed: {ex.Message}", "OK")!;
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
