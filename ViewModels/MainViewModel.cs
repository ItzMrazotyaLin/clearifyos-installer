using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClearifyOS_Installer.ViewModels;

/// <summary>
/// Main view model for the ClearifyOS online installer.
/// Placeholder navigation commands wired to the footer bar.
/// Note: "About" is shown as an in-app ContentDialog from
/// MainWindow code-behind (AboutButton_Click) so the main window
/// keeps DWM focus and the acrylic backdrop stays vibrant.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    [RelayCommand]
    private void Next()
    {
        // TODO: navigate to next installer step.
    }

    [RelayCommand]
    private void Cancel()
    {
        // TODO: cancel installation and close the app.
    }
}
