using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Float.UI.ViewModels.Wizards;

namespace Float.UI.Views.Wizards;

public partial class CreateContainerWizardView : UserControl
{
    public CreateContainerWizardView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(System.EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is CreateContainerWizardViewModel vm)
        {
            vm.RequestFolderPick += OnRequestFolderPick;
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(CreateContainerWizardViewModel.Output))
                    ConsoleScroller.ScrollToEnd();
            };
        }
    }

    private async void OnRequestFolderPick(VolumeItem item)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return;

        var results = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Host Directory",
            AllowMultiple = false,
        });

        if (results.Count > 0)
            item.HostPath = results[0].Path.LocalPath;
    }
}
