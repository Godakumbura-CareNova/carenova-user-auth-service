using UserAuth.Api.Models;

namespace UserAuth.Api.Repositories;

public interface IPasswordResetRepository
{
    Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default);
    Task<PasswordResetToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task MarkAsUsedAsync(PasswordResetToken token, CancellationToken cancellationToken = default);
}
