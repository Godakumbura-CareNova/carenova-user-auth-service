using Microsoft.EntityFrameworkCore;
using UserAuth.Api.Data;
using UserAuth.Api.Models;

namespace UserAuth.Api.Repositories;

public class PasswordResetRepository : IPasswordResetRepository
{
    private readonly AppDbContext _context;

    public PasswordResetRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
    {
        await _context.PasswordResetTokens.AddAsync(token, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PasswordResetToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);
    }

    public async Task MarkAsUsedAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
    {
        token.IsUsed = true;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
