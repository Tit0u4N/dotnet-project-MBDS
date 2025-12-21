using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gauniv.Client.Services;
using Gauniv.Client.Proxy;

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

        public ProfileViewModel()
        {
            // Charger la valeur persistée ou la valeur par défaut
            DownloadPath = DownloadService.Instance.GetDownloadPathFromPreferences();

            ChangeEmailCommand = new AsyncRelayCommand(ChangeEmailAsync);
            ChangePasswordCommand = new AsyncRelayCommand(ChangePasswordAsync);
            SetDownloadPathCommand = new AsyncRelayCommand(SetDownloadPathAsync);
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
                // Hide the toast after a short delay
                await Task.Delay(3000);
                IsToastVisible = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Change email failed: {ex.Message}");
                await Application.Current?.Windows[0]?.Page?.DisplayAlertAsync("Error", $"Change email failed: {ex.Message}", "OK")!;
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
                // Hide the toast after a short delay
                await Task.Delay(3000);
                IsToastVisible = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Change password failed: {ex.Message}");
                await Application.Current?.Windows[0]?.Page?.DisplayAlertAsync("Error", $"Change password failed: {ex.Message}", "OK")!;
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
                Console.WriteLine($"Save download path failed: {ex.Message}");
            }

            await Task.CompletedTask;
        }
    }
}
