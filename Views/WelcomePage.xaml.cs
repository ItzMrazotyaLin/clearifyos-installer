using ClearifyOS_Installer.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ClearifyOS_Installer.Views;

/// <summary>Wizard step 0: welcome screen with Fluent entrance motion.</summary>
public sealed partial class WelcomePage : Page
{
    public WelcomePage()
    {
        InitializeComponent();
    }

    private void WelcomePanel_Loaded(object sender, RoutedEventArgs e)
    {
        EntranceAnimator.PlaySlideUpEntrance(WelcomePanel, WelcomeTranslate);
    }
}
