using CommunityToolkit.Mvvm.Input;

namespace Float.UI.ViewModels;

public interface IHasPendingDelete
{
    bool HasPendingDelete { get; }
    string PendingDeleteName { get; }
    IRelayCommand CancelDeleteCommand { get; }
    IAsyncRelayCommand ConfirmDeleteCommand { get; }
}
