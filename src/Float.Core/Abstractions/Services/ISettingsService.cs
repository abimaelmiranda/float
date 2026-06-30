using Float.Core.Models;

namespace Float.Core.Abstractions.Services;

public interface ISettingsService
{
    string? LoadWarning { get; }
    AppSettings Get();
    void Save(AppSettings settings);
}
