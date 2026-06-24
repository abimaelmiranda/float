using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Float.Core.Abstractions.Services;

namespace Float.UI.ViewModels.Setup;

public partial class EngineSetupViewModel : ProvisioningViewModelBase
{
    public event EventHandler? SetupCompleted;

    private readonly IEngineProvisioner _provisioner;

    public EngineSetupViewModel(IEngineProvisioner provisioner)
    {
        _provisioner = provisioner;
    }

    protected override Task ProvisionAsync(IProgress<string> progress, CancellationToken cancellationToken)
        => _provisioner.InstallEngineAsync(progress, cancellationToken);

    [RelayCommand]
    private void Continue() => SetupCompleted?.Invoke(this, EventArgs.Empty);
}
