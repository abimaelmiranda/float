using CommunityToolkit.Mvvm.Input;

namespace Float.UI.Abstractions;

public interface IHasPendingDelete
{
    bool HasPendingDelete { get; }
    string PendingDeleteName { get; }
    IRelayCommand CancelDeleteCommand { get; }
    IAsyncRelayCommand ConfirmDeleteCommand { get; }
}
