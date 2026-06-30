using Avalonia.Controls;
using Float.UI.ViewModels.Wizards;

namespace Float.UI.Views.Wizards;

public partial class MigrationWizardView : UserControl
{
    public MigrationWizardView()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) =>
        {
            if (DataContext is MigrationWizardViewModel vm && vm.LoadDockerContainersCommand.CanExecute(null))
            {
                vm.LoadDockerContainersCommand.Execute(null);
            }
        };
    }
}
