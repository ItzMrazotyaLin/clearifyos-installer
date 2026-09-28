using ClearifyOS_Installer.Helpers;
using ClearifyOS_Installer.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Navigation;

namespace ClearifyOS_Installer.Views;

/// <summary>
/// Wizard step 1: disclaimer screen with Fluent entrance motion.
/// The agreement checkbox is bound two-way to the shared view model
/// (received as the navigation parameter) so the acceptance state —
/// and therefore the "Далее" availability — survives Back/Next
/// navigation, which creates a fresh page instance each time.
/// </summary>
public sealed partial class DisclaimerPage : Page
{
    public DisclaimerPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is MainViewModel viewModel)
        {
            AgreementCheckBox.SetBinding(
                CheckBox.IsCheckedProperty,
                new Binding
                {
                    Source = viewModel,
                    Path = new PropertyPath(nameof(MainViewModel.IsDisclaimerAccepted)),
                    Mode = BindingMode.TwoWay
                });
        }
    }

    private void DisclaimerPanel_Loaded(object sender, RoutedEventArgs e)
    {
        EntranceAnimator.PlaySlideUpEntrance(DisclaimerPanel, DisclaimerTranslate);
    }
}
