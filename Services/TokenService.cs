using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;

namespace Authentication.Identity.Services;

public interface ITokenService
{
    Task<string> GenerateRefreshTokenAsync();
    Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken);
    Task SaveRefreshTokenAsync(string userId, string refreshToken);
    Task RevokeRefreshTokenAsync(string userId);
}

public class TokenService : ITokenService
{
    private readonly UserManager<IdentityUser> _userManager;
    
    public TokenService(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }

    public Task<string> GenerateRefreshTokenAsync()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Task.FromResult(Convert.ToBase64String(randomNumber));
    }

    public async Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        var storedToken = await _userManager.GetAuthenticationTokenAsync(
            user, "Default", "RefreshToken");

        return storedToken == refreshToken;
    }

    public async Task SaveRefreshTokenAsync(string userId, string refreshToken)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return;

        await _userManager.SetAuthenticationTokenAsync(
            user, "Default", "RefreshToken", refreshToken);
    }

    public async Task RevokeRefreshTokenAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return;

        await _userManager.RemoveAuthenticationTokenAsync(
            user, "Default", "RefreshToken");
    }
}
