using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ClearifyOS_Installer.Helpers;
using ClearifyOS_Installer.ViewModels;
using ClearifyOS_Installer.Views;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Graphics;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ClearifyOS_Installer;

/// <summary>
/// Main installer window: DesktopAcrylic backdrop, custom title bar,
/// fixed 800x600 centered frame with the maximize box fully removed
/// at the native Win32 style level. Hosts the wizard Frame and keeps
/// the PipsPager step indicator in sync with the view model.
/// </summary>
public sealed partial class MainWindow : Microsoft.UI.Xaml.Window
{
    public MainViewModel ViewModel { get; } = new();

    private int _currentStep = -1;
    private bool _isNavigating;
    private bool _exitConfirmed;
    private bool _isConfirmShowing;

    private const int GWL_STYLE = -16;
    private const nint WS_MAXIMIZEBOX = 0x00010000;
    private const nint WS_THICKFRAME = 0x00040000;

    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_FRAMECHANGED = 0x0020;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern nint GetWindowLongPtr(nint hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        nint hWnd,
        nint hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);

    public MainWindow()
    {
        InitializeComponent();
        SystemBackdrop = new DesktopAcrylicBackdrop();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(CustomTitleBar);

        AppWindow.SetIcon("Assets/AppIcon.ico");
        var titleBar = AppWindow.TitleBar;
        titleBar.ButtonBackgroundColor = Colors.Transparent;
        titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        titleBar.ButtonHoverBackgroundColor = ColorHelper.FromArgb(0x20, 0xFF, 0xFF, 0xFF);
        titleBar.ButtonPressedBackgroundColor = ColorHelper.FromArgb(0x30, 0xFF, 0xFF, 0xFF);
        titleBar.ButtonForegroundColor = Colors.White;
        titleBar.ButtonInactiveForegroundColor = Colors.Gray;
        titleBar.ButtonHoverForegroundColor = Colors.White;
        titleBar.ButtonPressedForegroundColor = Colors.White;

        const int width = 800;
        const int height = 600;

        if (AppWindow.Presenter is OverlappedPresenter overlapped)
        {
            overlapped.IsResizable = false;
            overlapped.IsMaximizable = false;
            overlapped.IsMinimizable = true;
        }
        else
        {
            var presenter = OverlappedPresenter.Create();
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = true;
            AppWindow.SetPresenter(presenter);
        }

        RemoveMaximizeBoxAndSizingBorder();

        AppWindow.Resize(new SizeInt32(width, height));

        CenterOnActiveDisplay(width, height);

        // Wizard wiring: view model owns the step, the window drives the Frame.
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        ViewModel.CancelRequested += ViewModel_CancelRequested;

        // Intercept the native title-bar close button (X) for confirmation.
        AppWindow.Closing += AppWindow_Closing;

        // Initial step without a transition (window is still fading in).
        ContentFrame.Navigate(typeof(WelcomePage), null, new SuppressNavigationTransitionInfo());
        _currentStep = ViewModel.StepIndex;
    }

    private void RemoveMaximizeBoxAndSizingBorder()
    {
        nint hwnd = WindowNative.GetWindowHandle(this);
        if (hwnd == nint.Zero)
        {
            return;
        }

        nint style = GetWindowLongPtr(hwnd, GWL_STYLE);
        nint newStyle = style & ~WS_MAXIMIZEBOX & ~WS_THICKFRAME;
        if (newStyle != style)
        {
            SetWindowLongPtr(hwnd, GWL_STYLE, newStyle);
            SetWindowPos(hwnd, nint.Zero, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
        }
    }

    private void CenterOnActiveDisplay(int windowWidth, int windowHeight)
    {
        DisplayArea displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest);

        RectInt32 workArea = displayArea.WorkArea;
        int x = workArea.X + (workArea.Width - windowWidth) / 2;
        int y = workArea.Y + (workArea.Height - windowHeight) / 2;
        AppWindow.Move(new PointInt32(x, y));
    }

    private void RootGrid_Loaded(object sender, RoutedEventArgs e)
    {
        // Backdrop fade-in masks the fallback-color to acrylic pop.
        EntranceAnimator.PlayFadeIn(RootGrid);
    }

    private async void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.StepIndex))
        {
            await NavigateToStepAsync();
        }
    }

    /// <summary>
    /// Vertical step switch: the outgoing view drifts down +20px while
    /// fading out (~250ms); only then is the incoming page navigated in
    /// with a suppressed built-in transition so its own Loaded entrance
    /// (fade-in rising +24px to 0, ~300ms) carries the motion. Same
    /// language forward and backward. Serialized with latest-wins so
    /// rapid step changes cannot desync the Frame from the view model.
    /// </summary>
    private async Task NavigateToStepAsync()
    {
        if (_isNavigating)
        {
            return;
        }

        _isNavigating = true;
        try
        {
            while (ViewModel.StepIndex != _currentStep)
            {
                int target = Math.Clamp(ViewModel.StepIndex, 0, ViewModel.TotalSteps - 1);

                if (ContentFrame.Content is FrameworkElement outgoing)
                {
                    await EntranceAnimator.PlaySlideDownExitAsync(outgoing);
                    // Re-read: the step may have changed mid-exit.
                    target = Math.Clamp(ViewModel.StepIndex, 0, ViewModel.TotalSteps - 1);
                }

                var pageType = target == 0 ? typeof(WelcomePage) : typeof(DisclaimerPage);
                // Disclaimer receives the shared view model so its agreement
                // checkbox binds two-way to IsDisclaimerAccepted.
                object? parameter = target == 0 ? null : ViewModel;
                ContentFrame.Navigate(pageType, parameter, new SuppressNavigationTransitionInfo());
                _currentStep = target;
            }
        }
        finally
        {
            _isNavigating = false;
        }
    }

    private void StepPips_SelectedIndexChanged(PipsPager sender, PipsPagerSelectedIndexChangedEventArgs args)
    {
        int index = Math.Clamp(sender.SelectedPageIndex, 0, ViewModel.TotalSteps - 1);
        if (index != ViewModel.StepIndex)
        {
            // Routes through the view model so Frame + indicator stay in sync.
            ViewModel.StepIndex = index;
        }
    }

    private async void ViewModel_CancelRequested(object? sender, EventArgs e)
    {
        if (await ConfirmExitAsync())
        {
            _exitConfirmed = true;
            Application.Current.Exit();
        }
    }

    /// <summary>
    /// Intercepts the native title-bar close button (X): cancels the
    /// close and routes through the same exit confirmation dialog as
    /// the "Отмена"/"Назад" footer flow.
    /// </summary>
    private async void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_exitConfirmed)
        {
            return;
        }

        args.Cancel = true;

        if (_isConfirmShowing)
        {
            return;
        }

        _isConfirmShowing = true;
        try
        {
            if (await ConfirmExitAsync())
            {
                _exitConfirmed = true;
                Application.Current.Exit();
            }
        }
        finally
        {
            _isConfirmShowing = false;
        }
    }

    /// <summary>
    /// Shared exit confirmation used by both the footer secondary action
    /// (Step 1) and the native close button. Returns true for "Да, выйти".
    /// </summary>
    private async Task<bool> ConfirmExitAsync()
    {
        var dialog = new ContentDialog
        {
            Title = "Отмена установки",
            Content = "Вы уверены, что хотите прервать установку и закрыть приложение?",
            PrimaryButtonText = "Да, выйти",
            CloseButtonText = "Остаться",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = Content.XamlRoot
        };

        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    private async void AboutButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AboutDialog
        {
            XamlRoot = Content.XamlRoot
        };
        await dialog.ShowAsync();
    }
}
