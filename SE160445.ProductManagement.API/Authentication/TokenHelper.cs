using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

public static class TokenHelper
{
    public static string GenerateAccessToken(IEnumerable<Claim> claims, string secretKey, string issuer, string audience, int expiryMinutes)
    {
        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.Now.AddMinutes(expiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static (string token, DateTime expiration) GenerateRefreshToken(string secretKey, string issuer, string audience, int expiryMinutes)
    {
        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenExpiration = DateTime.Now.AddMinutes(expiryMinutes);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            expires: tokenExpiration,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), tokenExpiration);
    }
}
