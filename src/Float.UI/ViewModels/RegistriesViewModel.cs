using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Float.Core.Abstractions.Services;
using Float.Core.Models;

namespace Float.UI.ViewModels;

public partial class RegistryItemViewModel : ViewModelBase
{
    public RegistryInfo Source { get; }
    public string Hostname => Source.Hostname;
    public string? Username => Source.Username;
    public bool HasUsername => Username is not null;

    internal Action<RegistryItemViewModel>? RequestLogoutAction { get; set; }

    public RegistryItemViewModel(RegistryInfo source)
    {
        Source = source;
    }

    [RelayCommand]
    private void RequestLogout() => RequestLogoutAction?.Invoke(this);
}

public partial class RegistriesViewModel : ViewModelBase
{
    private readonly IRegistryService _registryService;

    public ObservableCollection<RegistryItemViewModel> Registries { get; } = [];

    [ObservableProperty] public partial bool IsLoginFormVisible { get; set; }
    [ObservableProperty] public partial string LoginServer { get; set; } = "";
    [ObservableProperty] public partial string LoginUsername { get; set; } = "";
    [ObservableProperty] public partial string LoginPassword { get; set; } = "";
    [ObservableProperty] public partial string LoginScheme { get; set; } = "auto";
    [ObservableProperty] public partial bool IsLoggingIn { get; set; }
    [ObservableProperty] public partial bool HasLoginError { get; set; }
    [ObservableProperty] public partial string LoginError { get; set; } = "";
    [ObservableProperty] public partial bool HasError { get; set; }
    [ObservableProperty] public partial string ErrorMessage { get; set; } = "";
    [ObservableProperty] public partial RegistryItemViewModel? PendingLogout { get; set; }

    public bool HasPendingLogout => PendingLogout is not null;
    public string PendingLogoutName => PendingLogout?.Hostname ?? "";

    public string[] SchemeOptions { get; } = ["auto", "https", "http"];

    public RegistriesViewModel(IRegistryService registryService)
    {
        _registryService = registryService;
    }

    partial void OnPendingLogoutChanged(RegistryItemViewModel? value)
    {
        OnPropertyChanged(nameof(HasPendingLogout));
        OnPropertyChanged(nameof(PendingLogoutName));
    }

    [RelayCommand]
    private Task RefreshRegistriesAsync() => RefreshAsync();

    [RelayCommand]
    private void ShowLoginForm()
    {
        LoginServer = "";
        LoginUsername = "";
        LoginPassword = "";
        LoginScheme = "auto";
        HasLoginError = false;
        LoginError = "";
        IsLoginFormVisible = true;
    }

    [RelayCommand]
    private void CancelLogin()
    {
        IsLoginFormVisible = false;
        LoginPassword = "";
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(LoginServer) || string.IsNullOrWhiteSpace(LoginUsername))
            return;

        IsLoggingIn = true;
        HasLoginError = false;
        LoginError = "";

        var result = await _registryService.LoginAsync(
            LoginServer.Trim(), LoginUsername.Trim(), LoginPassword, LoginScheme)
            .ConfigureAwait(false);

        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            IsLoggingIn = false;
            if (result.IsFailure)
            {
                HasLoginError = true;
                LoginError = result.Failure.Message ?? "Login failed";
                return;
            }

            IsLoginFormVisible = false;
            LoginPassword = "";
            await RefreshAsync();
        });
    }

    [RelayCommand]
    private void CancelLogout() => PendingLogout = null;

    [RelayCommand]
    private async Task ConfirmLogoutAsync()
    {
        var registry = PendingLogout;
        PendingLogout = null;
        if (registry is null) return;

        var result = await _registryService.LogoutAsync(registry.Hostname).ConfigureAwait(false);
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            if (result.IsFailure)
            {
                HasError = true;
                ErrorMessage = result.Failure.Message ?? "Logout failed";
                return;
            }

            await RefreshAsync();
        });
    }

    public async Task RefreshAsync()
    {
        HasError = false;
        var result = await _registryService.ListAsync().ConfigureAwait(false);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (result.IsFailure)
            {
                HasError = true;
                ErrorMessage = result.Failure.Message ?? "Failed to load registries";
                return;
            }

            Registries.Clear();
            foreach (var info in result.GetValueOrThrow())
            {
                var vm = new RegistryItemViewModel(info);
                vm.RequestLogoutAction = r => PendingLogout = r;
                Registries.Add(vm);
            }
        });
    }
}
