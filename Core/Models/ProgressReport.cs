namespace LukkyBoost.App.Core.Models;

public class ProgressReport
{
    public double Percentage { get; set; }
    public string StepMessage { get; set; } = string.Empty;
    public string? CompletedLog { get; set; }
}

