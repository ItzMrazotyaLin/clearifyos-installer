using Microsoft.UI.Xaml.Controls;

namespace ClearifyOS_Installer.Views;

/// <summary>
/// In-app About dialog. Shown via ContentDialog so the main window
/// keeps DWM focus and the DesktopAcrylic backdrop stays vibrant
/// (a separate top-level window would dim it to dull grey).
/// XamlRoot must be assigned by the caller before ShowAsync.
/// </summary>
public sealed partial class AboutDialog : ContentDialog
{
    public AboutDialog()
    {
        InitializeComponent();
    }
}
