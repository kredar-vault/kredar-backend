using Kredar.API.Data;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.Auth;

public class RefreshTokenRepository(AppDbContext db)
{
    public async Task AddAsync(RefreshToken token)
    {
        await db.RefreshTokens.AddAsync(token);
        await db.SaveChangesAsync();
    }

    public async Task<RefreshToken?> FindByTokenAsync(string token) =>
        await db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == token && !r.IsRevoked);

    public async Task RevokeAsync(RefreshToken token)
    {
        token.IsRevoked = true;
        await db.SaveChangesAsync();
    }
}
