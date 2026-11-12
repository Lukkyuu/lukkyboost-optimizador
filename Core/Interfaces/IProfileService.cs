using LukkyBoost.App.Core.Enums;

namespace LukkyBoost.App.Core.Interfaces;

public interface IProfileService
{
    PerformanceProfile CurrentProfile { get; }
    Task<PerformanceProfile> LoadLastSavedProfileAsync();
    Task<string> ApplyProfileAsync(PerformanceProfile profile, CancellationToken cancellationToken = default);
}

