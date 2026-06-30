using Float.Core.Models;
using Float.Core.Models.Results;

namespace Float.Core.Abstractions.Services;

public interface IRegistryService
{
    Task<Result<IReadOnlyList<RegistryInfo>>> ListAsync(CancellationToken cancellationToken = default);

    Task<Result> LoginAsync(
        string server,
        string username,
        string password,
        string scheme = "auto",
        CancellationToken cancellationToken = default);

    Task<Result> LogoutAsync(string server, CancellationToken cancellationToken = default);
}
