using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Float.Core.Models.Results.Errors;

namespace Float.UI.ViewModels;

public partial class CrashHandlerViewModel : ObservableObject
{
    public string Title { get; }
    public string Message { get; }
    public string? Details { get; }
    public Error? Error { get; }
    public bool HasDetails => !string.IsNullOrWhiteSpace(Details);

    public string CopyText => string.IsNullOrWhiteSpace(Details)
        ? $"{Title}\n\n{Message}"
        : $"{Title}\n\n{Message}\n\n{Details}";

    public CrashHandlerViewModel(string title, string message, Error? error = null)
    {
        Title = title;
        Message = message;
        Error = error;
        Details = error?.Code is { Length: > 0 }
            ? $"Error Code: {error.Code}"
            : null;
    }
}
