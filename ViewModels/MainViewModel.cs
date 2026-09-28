using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;

namespace ClearifyOS_Installer.ViewModels;

/// <summary>
/// Main view model for the ClearifyOS online installer wizard.
/// Owns the current step index; the view (MainWindow) observes it
/// and drives Frame navigation + transitions.
/// Footer state is step-aware and exposed declaratively:
/// Step 0 shows "О программе" + "Отмена" (exit flow);
/// Step 1+ collapses "О программе" and offers "Назад" (back nav).
/// Note: dialogs are shown as in-app ContentDialogs from MainWindow
/// code-behind so the main window keeps DWM focus and the acrylic
/// backdrop stays vibrant.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private int _stepIndex;
    private bool _isDisclaimerAccepted;

    /// <summary>0 = Welcome, 1 = Disclaimer.</summary>
    public int StepIndex
    {
        get => _stepIndex;
        set
        {
            if (SetProperty(ref _stepIndex, value))
            {
                OnPropertyChanged(nameof(StepText));
                OnPropertyChanged(nameof(IsFirstStep));
                OnPropertyChanged(nameof(IsLastStep));
                OnPropertyChanged(nameof(AboutVisibility));
                OnPropertyChanged(nameof(SecondaryButtonText));
                OnPropertyChanged(nameof(IsNextEnabled));
            }
        }
    }

    /// <summary>
    /// Disclaimer agreement. Lives here (not in the page) so it survives
    /// Back/Next navigation — each Frame navigation creates a fresh page
    /// instance, but the view model persists.
    /// </summary>
    public bool IsDisclaimerAccepted
    {
        get => _isDisclaimerAccepted;
        set
        {
            if (SetProperty(ref _isDisclaimerAccepted, value))
            {
                OnPropertyChanged(nameof(IsNextEnabled));
            }
        }
    }

    /// <summary>
    /// "Далее" availability: always on for Welcome, gated on the
    /// agreement checkbox for Disclaimer.
    /// </summary>
    public bool IsNextEnabled => IsFirstStep || IsDisclaimerAccepted;

    public int TotalSteps => 2;

    public string StepText => $"Шаг {StepIndex + 1} из {TotalSteps}";

    public bool IsFirstStep => StepIndex == 0;

    public bool IsLastStep => StepIndex == TotalSteps - 1;

    public Visibility AboutVisibility => IsFirstStep ? Visibility.Visible : Visibility.Collapsed;

    public string SecondaryButtonText => IsFirstStep ? "Отмена" : "Назад";

    /// <summary>Raised when the user requests app exit; the view shows a confirmation dialog.</summary>
    public event EventHandler? CancelRequested;

    [RelayCommand]
    private void Next()
    {
        if (StepIndex < TotalSteps - 1)
        {
            StepIndex++;
        }
    }

    /// <summary>
    /// Step-aware secondary action: back navigation on Step 2+,
    /// exit confirmation flow on Step 1.
    /// </summary>
    [RelayCommand]
    private void SecondaryAction()
    {
        if (StepIndex > 0)
        {
            StepIndex--;
        }
        else
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
