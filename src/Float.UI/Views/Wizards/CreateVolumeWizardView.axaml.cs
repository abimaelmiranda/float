using Avalonia.Controls;
using Float.UI.ViewModels.Wizards;

namespace Float.UI.Views.Wizards;

public partial class CreateVolumeWizardView : UserControl
{
    public CreateVolumeWizardView()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            if (DataContext is CreateVolumeWizardViewModel vm)
                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(CreateVolumeWizardViewModel.Output))
                        OutputBox.CaretIndex = OutputBox.Text?.Length ?? 0;
                };
        };
    }
}
