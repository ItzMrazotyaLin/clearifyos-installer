using System;
using System.Runtime.InteropServices;
using ClearifyOS_Installer.ViewModels;
using ClearifyOS_Installer.Views;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Graphics;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ClearifyOS_Installer;

/// <summary>
/// </summary>
public sealed partial class MainWindow : Microsoft.UI.Xaml.Window
{
    public MainViewModel ViewModel { get; } = new();

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
        var rootFade = new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(350),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
        };
        var rootStoryboard = new Storyboard();
        Storyboard.SetTarget(rootFade, RootGrid);
        Storyboard.SetTargetProperty(rootFade, "Opacity");
        rootStoryboard.Children.Add(rootFade);
        rootStoryboard.Begin();

        var easeOut = new CubicEase { EasingMode = EasingMode.EaseOut };

        var textFade = new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(500),
            EasingFunction = easeOut
        };
        Storyboard.SetTarget(textFade, WelcomePanel);
        Storyboard.SetTargetProperty(textFade, "Opacity");

        var slideUp = new DoubleAnimation
        {
            From = 24.0,
            To = 0.0,
            Duration = TimeSpan.FromMilliseconds(500),
            EasingFunction = easeOut
        };
        Storyboard.SetTarget(slideUp, WelcomeTranslate);
        Storyboard.SetTargetProperty(slideUp, "Y");

        var welcomeStoryboard = new Storyboard();
        welcomeStoryboard.Children.Add(textFade);
        welcomeStoryboard.Children.Add(slideUp);
        welcomeStoryboard.Begin();
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
