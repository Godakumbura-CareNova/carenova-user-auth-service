using Microsoft.EntityFrameworkCore;
using UserAuth.Api.Data;
using UserAuth.Api.DTOs.Dashboard;
using UserAuth.Api.Models;

namespace UserAuth.Api.Services;

public sealed class AdminUserDashboardService(AppDbContext db)
{
    public async Task<AdminUserDashboardDto> GetAsync(CancellationToken ct)
    {
        var groups = await db.Users.AsNoTracking().GroupBy(u => u.Role)
            .Select(g => new { Role = g.Key, Count = g.Count() }).ToListAsync(ct);
        var counts = new Dictionary<string, int>();
        foreach (var group in groups)
            if (RoleNames.TryNormalize(group.Role, out var role)) counts[role] = counts.GetValueOrDefault(role) + group.Count;
        return new(groups.Sum(g => g.Count), counts.GetValueOrDefault(RoleNames.Patient),
            counts.GetValueOrDefault(RoleNames.PharmacyStaff), counts.GetValueOrDefault(RoleNames.Doctor), DateTime.UtcNow);
    }
}
