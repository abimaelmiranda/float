using Avalonia.Controls;
using Avalonia.Threading;
using Float.Core.Abstractions.Services;
using Float.Core.Models.Results.Errors;
using Float.UI.ViewModels;
using Float.UI.Views;

namespace Float.UI.Services;

public sealed class UserNotificationService : IUserNotificationService
{
    private Window? _mainWindow;

    public void SetMainWindow(Window mainWindow) => _mainWindow = mainWindow;

    public void ShowError(string title, string message, Error? error = null)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var vm = new CrashHandlerViewModel(title, message, error);
            var window = new CrashHandlerWindow
            {
                DataContext = vm,
                Title = title
            };

            if (_mainWindow is not null)
                window.ShowDialog(_mainWindow);
            else
                window.Show();
        });
    }

    public void ShowWarning(string title, string message)
    {
        ShowError(title, message);
    }

    public void ShowInfo(string title, string message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var vm = new CrashHandlerViewModel(title, message);
            var window = new CrashHandlerWindow
            {
                DataContext = vm,
                Title = title
            };

            if (_mainWindow is not null)
                window.ShowDialog(_mainWindow);
            else
                window.Show();
        });
    }
}
