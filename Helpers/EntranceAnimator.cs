using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace ClearifyOS_Installer.Helpers;

/// <summary>
/// Shared Fluent entrance motions: window backdrop fade-in plus the
/// vertical slide/fade choreography used for wizard step switches.
/// Forward switch: outgoing view drifts down +20px while fading out
/// (~250ms), then the incoming view fades in rising +24px to 0
/// (~300ms). Backward switches reuse the same vertical language.
/// </summary>
public static class EntranceAnimator
{
    /// <summary>
    /// Fades <paramref name="element"/> from 0 to 1 to mask the
    /// fallback-color to translucent-backdrop pop on window init.
    /// </summary>
    public static void PlayFadeIn(FrameworkElement element, int durationMs = 350)
    {
        var fade = new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(durationMs),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
        };

        var storyboard = new Storyboard();
        Storyboard.SetTarget(fade, element);
        Storyboard.SetTargetProperty(fade, "Opacity");
        storyboard.Children.Add(fade);
        storyboard.Begin();
    }

    /// <summary>
    /// Fluent entrance: fades <paramref name="panel"/> 0 to 1 while
    /// sliding <paramref name="translate"/> +24px to 0, ease-out.
    /// The panel must start at Opacity="0" with its
    /// <paramref name="translate"/> at Y="24".
    /// </summary>
    public static void PlaySlideUpEntrance(FrameworkElement panel, TranslateTransform translate, int durationMs = 300)
    {
        var easeOut = new CubicEase { EasingMode = EasingMode.EaseOut };

        var fade = new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(durationMs),
            EasingFunction = easeOut
        };
        Storyboard.SetTarget(fade, panel);
        Storyboard.SetTargetProperty(fade, "Opacity");

        var slide = new DoubleAnimation
        {
            From = 24.0,
            To = 0.0,
            Duration = TimeSpan.FromMilliseconds(durationMs),
            EasingFunction = easeOut
        };
        Storyboard.SetTarget(slide, translate);
        Storyboard.SetTargetProperty(slide, "Y");

        var storyboard = new Storyboard();
        storyboard.Children.Add(fade);
        storyboard.Children.Add(slide);
        storyboard.Begin();
    }

    /// <summary>
    /// Exit phase of a step switch: <paramref name="element"/> drifts
    /// down to +20px while fading 1 to 0, ease-in. Awaiting the
    /// returned task guarantees the outgoing view is gone before the
    /// incoming view is navigated in.
    /// </summary>
    public static Task PlaySlideDownExitAsync(FrameworkElement element, int durationMs = 250)
    {
        if (element.RenderTransform is not TranslateTransform translate)
        {
            translate = new TranslateTransform();
            element.RenderTransform = translate;
        }

        var tcs = new TaskCompletionSource<object?>();

        var easeIn = new CubicEase { EasingMode = EasingMode.EaseIn };

        var fade = new DoubleAnimation
        {
            From = 1.0,
            To = 0.0,
            Duration = TimeSpan.FromMilliseconds(durationMs),
            EasingFunction = easeIn
        };
        Storyboard.SetTarget(fade, element);
        Storyboard.SetTargetProperty(fade, "Opacity");

        var drop = new DoubleAnimation
        {
            From = translate.Y,
            To = 20.0,
            Duration = TimeSpan.FromMilliseconds(durationMs),
            EasingFunction = easeIn
        };
        Storyboard.SetTarget(drop, translate);
        Storyboard.SetTargetProperty(drop, "Y");

        var storyboard = new Storyboard();
        storyboard.Children.Add(fade);
        storyboard.Children.Add(drop);
        storyboard.Completed += (_, _) => tcs.TrySetResult(null);
        storyboard.Begin();

        return tcs.Task;
    }
}
