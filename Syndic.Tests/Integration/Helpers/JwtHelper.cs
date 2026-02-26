using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Syndic.Tests.Integration.Helpers;

public static class JwtHelper
{
    /// <summary>
    /// Mints a signed HS256 JWT that matches the backend's expected format:
    /// issuer "internal-auth", audience "aspnet-api", sub = email.
    /// </summary>
    public static string MintToken(
        string email,
        string provider,
        string secret,
        string name = "Test User",
        int expiryMinutes = 5)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Mirror the claim names used by the backend (Constants.cs + UserMiddleware)
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, email),
            new Claim("provider", provider),
            new Claim("name", name),
        };

        var token = new JwtSecurityToken(
            issuer: "internal-auth",
            audience: "aspnet-api",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static AuthenticationHeaderValue BearerHeader(
        string email,
        string provider,
        string secret) =>
        new("Bearer", MintToken(email, provider, secret));
}
