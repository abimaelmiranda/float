using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Float.UI.ViewModels;

namespace Float.UI.Views;

public partial class CrashHandlerWindow : Window
{
    public CrashHandlerWindow()
    {
        InitializeComponent();
    }

    private async void OnCopyDetails(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CrashHandlerViewModel vm && Clipboard is not null)
        {
            var item = new DataTransferItem();
            item.SetText(vm.CopyText);
            var transfer = new DataTransfer();
            transfer.Add(item);
            await Clipboard.SetDataAsync(transfer);
        }
    }

    private void OnClose(object? sender, RoutedEventArgs e) => Close();
}
