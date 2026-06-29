using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Float.UI.Resources;
using Float.UI.ViewModels;
using Float.UI.ViewModels.Setup;
using Float.UI.ViewModels.Wizards;

namespace Float.UI;

public class ViewLocator : IDataTemplate
{
    public static IServiceProvider? Services { get; set; }

    private static readonly Dictionary<Type, Func<Control>> _map = new()
    {
        [typeof(DashboardViewModel)]             = () => new Views.DashboardView(),
        [typeof(ImagesViewModel)]                = () => new Views.ImagesView(),
        [typeof(VolumesViewModel)]               = () => new Views.VolumesView(),
        [typeof(MigrationWizardViewModel)]       = () => new Views.Wizards.MigrationWizardView(),
        [typeof(EngineSetupViewModel)]           = () => new Views.Setup.EngineSetupView(),
        [typeof(CreateContainerWizardViewModel)] = () => new Views.Wizards.CreateContainerWizardView(),
        [typeof(CreateVolumeWizardViewModel)]    = () => new Views.Wizards.CreateVolumeWizardView(),
        [typeof(SettingsViewModel)]              = () => new Views.SettingsView(),
        [typeof(RegistriesViewModel)]            = () => new Views.RegistriesView(),
    };

    public Control? Build(object? param)
    {
        if (param is null) return null;
        return _map.TryGetValue(param.GetType(), out var factory)
            ? factory()
            : new TextBlock { Text = string.Format(UiStrings.Get("NotFoundFormat"), param.GetType().Name) };
    }

    public bool Match(object? data) => data is ViewModelBase;
}
