using Avalonia;
using Avalonia.Controls;

namespace Float.UI.Views.Shared;

public partial class ConfirmDeleteDialog : UserControl
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<ConfirmDeleteDialog, string>(nameof(Title));

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public ConfirmDeleteDialog()
    {
        InitializeComponent();
    }
}
