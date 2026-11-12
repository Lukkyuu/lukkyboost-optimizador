using CommunityToolkit.Mvvm.ComponentModel;
using LukkyBoost.App.Core.Models;

namespace LukkyBoost.App.ViewModels;

public sealed partial class ProgressModalViewModel : ObservableObject
{
    [ObservableProperty]
    private string _operationTitle = "Procesando operación...";

    [ObservableProperty]
    private string _currentStepDescription = "Inicializando motor de ejecución...";

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private string _lastCompletedLog = "✔ Preparando entorno seguro...";

    public ProgressModalViewModel(string operationTitle = "Procesando operación...")
    {
        _operationTitle = operationTitle;
    }

    /// <summary>
    /// Crea una instancia de <see cref="IProgress{ProgressReport}"/> acoplada al SynchronizationContext
    /// del hilo UI (Dispatcher) para actualizar el modal desde Task.Run sin bloquear animaciones.
    /// </summary>
    public IProgress<ProgressReport> CreateProgressReporter()
    {
        return new Progress<ProgressReport>(ApplyReport);
    }

    public void ApplyReport(ProgressReport report)
    {
        ProgressPercentage = Math.Clamp(Math.Round(report.Percentage, 0), 0, 100);

        if (!string.IsNullOrWhiteSpace(report.StepMessage))
        {
            CurrentStepDescription = report.StepMessage;
        }

        if (!string.IsNullOrWhiteSpace(report.CompletedLog))
        {
            LastCompletedLog = report.CompletedLog;
        }
    }
}
