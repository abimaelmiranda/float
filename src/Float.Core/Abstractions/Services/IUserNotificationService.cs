using Float.Core.Models.Results.Errors;

namespace Float.Core.Abstractions.Services;

public interface IUserNotificationService
{
    void ShowError(string title, string message, Error? error = null);
    void ShowWarning(string title, string message);
    void ShowInfo(string title, string message);
}
