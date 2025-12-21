namespace Gauniv.Client.Services;

public class AlertService
{
    public static AlertService Instance { get; } = new AlertService();
    
    public Task ShowAlertAsync(string title, string message, string cancel = "OK")
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page == null)
        {
            System.Diagnostics.Debug.WriteLine($"{title}: {message}");
            return Task.CompletedTask;
        }

        // InvokeOnMainThreadAsync avoids async void
        return MainThread.InvokeOnMainThreadAsync(() => page.DisplayAlertAsync(title, message, cancel));
    }
    
}