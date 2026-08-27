using Microsoft.EntityFrameworkCore;
using UserAuth.Api.Data;
using UserAuth.Api.Models;

namespace UserAuth.Api.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim();

        return await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email.ToLower() == normalizedEmail.ToLower(), cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLower();

        return await _context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);
    }

    public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }
}
