using System.Windows;
using System.Windows.Media.Effects;
using LukkyBoost.App.Core.Models;
using LukkyBoost.App.ViewModels;

namespace LukkyBoost.App.Views;

public partial class ProgressModalWindow : Window
{
    public ProgressModalWindow(ProgressModalViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    /// <summary>
    /// Abre la ventana modal de progreso, aplica desenfoque y oscurecimiento sobre MainWindow,
    /// ejecuta la operación asíncrona reportando progreso con <see cref="IProgress{ProgressReport}"/>
    /// y cierra el modal automáticamente al finalizar.
    /// </summary>
    public static async Task ExecuteAsync(
        string operationTitle,
        Func<IProgress<ProgressReport>, Task> workAction)
    {
        var vm = new ProgressModalViewModel(operationTitle);
        var modal = new ProgressModalWindow(vm);

        var owner = Application.Current?.MainWindow;
        Effect? previousEffect = null;
        double previousOpacity = 1.0;

        if (owner is not null && owner != modal && owner.IsLoaded)
        {
            modal.Owner = owner;
            previousEffect = owner.Effect;
            previousOpacity = owner.Opacity;

            owner.Effect = new BlurEffect
            {
                Radius = 8,
                KernelType = KernelType.Gaussian
            };
            owner.Opacity = 0.65;
        }
        else
        {
            modal.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        IProgress<ProgressReport> progress = vm.CreateProgressReporter();
        modal.Show();

        try
        {
            await workAction(progress);
            await Task.Delay(320);
        }
        finally
        {
            modal.Close();

            if (owner is not null && owner != modal && owner.IsLoaded)
            {
                owner.Effect = previousEffect;
                owner.Opacity = previousOpacity;
            }
        }
    }
}

