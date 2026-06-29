using Float.Core.Models;

namespace Float.Core.Abstractions.Services;

public interface ISettingsService
{
    AppSettings Get();
    void Save(AppSettings settings);
}
