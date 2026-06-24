using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Float.UI.ViewModels.Wizards;

public partial class MigrationWizardViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial int CurrentStep { get; set; } = 1;

    public int TotalSteps => 3;

    [RelayCommand]
    private void Next()
    {
        if (CurrentStep < TotalSteps)
            CurrentStep++;
    }

    [RelayCommand]
    private void Back()
    {
        if (CurrentStep > 1)
            CurrentStep--;
    }
}
