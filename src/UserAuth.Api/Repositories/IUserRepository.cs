using UserAuth.Api.Models;

namespace UserAuth.Api.Repositories;

public interface IUserRepository
{
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);
}
