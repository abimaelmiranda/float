using Avalonia.Controls;
using Float.UI.ViewModels.Setup;

namespace Float.UI.Views.Setup;

public partial class EngineSetupView : UserControl
{
    public EngineSetupView()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            if (DataContext is EngineSetupViewModel vm)
                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(EngineSetupViewModel.Output))
                        ConsoleScroller.ScrollToEnd();
                };
        };
    }
}
