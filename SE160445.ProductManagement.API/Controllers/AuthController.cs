using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SE160445.ProductManagement.Repo.DTOs.Auth;
using SE160445.ProductManagement.Repo.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace SE160445.ProductManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly JwtSettings _jwtSettings;
        private readonly IConfiguration _configuration;

        private readonly List<User> _fakeUsers = new List<User>
        {
            new User { Username = "string", Password = "string", Roles = new List<string> { "Admin" } },
            new User { Username = "user2", Password = "password2", Roles = new List<string> { "User" } }
        };

        private static Dictionary<string, (string token, DateTime expiration)> _refreshTokens = new Dictionary<string, (string token, DateTime expiration)>();

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
            _jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>();
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginModel loginModel)
        {
            var user = _fakeUsers.FirstOrDefault(u => u.Username == loginModel.Username && u.Password == loginModel.Password);

            if (user == null)
            {
                return Unauthorized();
            }

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

            if (string.IsNullOrEmpty(_jwtSettings.Key))
            {
                return BadRequest("JWT key is not configured.");
            }

            var accessToken = TokenHelper.GenerateAccessToken(claims, _jwtSettings.Key, _jwtSettings.Issuer, _jwtSettings.Audience, _jwtSettings.AccessTokenExpiryMinutes);

            // Check if user already has a valid refresh token
            if (!_refreshTokens.TryGetValue(user.Username, out var refreshTokenInfo) || refreshTokenInfo.expiration < DateTime.Now)
            {
                var (newRefreshToken, newRefreshTokenExpiration) = TokenHelper.GenerateRefreshToken(_jwtSettings.Key, _jwtSettings.Issuer, _jwtSettings.Audience, _jwtSettings.RefreshTokenExpiryMinutes);
                _refreshTokens[user.Username] = (newRefreshToken, newRefreshTokenExpiration);
                refreshTokenInfo = (newRefreshToken, newRefreshTokenExpiration);
            }

            return Ok(new
            {
                accessToken = "Bearer " + accessToken,
                refreshToken = refreshTokenInfo.token,
                accessTokenExpiration = DateTime.Now.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes),
                refreshTokenExpiration = refreshTokenInfo.expiration
            });
        }

        [HttpPost("refresh")]
        public IActionResult Refresh([FromBody] RefreshModel refreshModel)
        {
            if (refreshModel == null || string.IsNullOrEmpty(refreshModel.Username) || string.IsNullOrEmpty(refreshModel.AccessToken) || string.IsNullOrEmpty(refreshModel.RefreshToken))
            {
                return BadRequest("Invalid client request");
            }

            var accessToken = refreshModel.AccessToken;
            if (accessToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                accessToken = accessToken.Substring("Bearer ".Length).Trim();
            }

            ClaimsPrincipal principal = null;
            try
            {
                principal = GetPrincipalFromExpiredToken(accessToken);
            }
            catch (SecurityTokenException e)
            {
                return BadRequest($"Invalid access token or refresh token: {e.Message}");
            }

            var username = refreshModel.Username; 
            if (!_refreshTokens.TryGetValue(username, out var savedRefreshToken))
            {
                return BadRequest("Invalid refresh token");
            }

            if (savedRefreshToken.token != refreshModel.RefreshToken || savedRefreshToken.expiration < DateTime.Now)
            {
                return BadRequest("Invalid refresh token");
            }

            var newAccessToken = TokenHelper.GenerateAccessToken(principal.Claims, _jwtSettings.Key, _jwtSettings.Issuer, _jwtSettings.Audience, _jwtSettings.AccessTokenExpiryMinutes);

            return Ok(new
            {
                accessToken = newAccessToken,
                refreshToken = savedRefreshToken.token,
                accessTokenExpiration = DateTime.Now.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes),
                refreshTokenExpiration = savedRefreshToken.expiration
            });
        }




        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Key);

            // Loại bỏ phần "Bearer " nếu có
            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = token.Substring("Bearer ".Length).Trim();
            }

            try
            {
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false, 
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
                var jwtSecurityToken = securityToken as JwtSecurityToken;
                if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new SecurityTokenException("Invalid token");
                }

                return principal;
            }
            catch (Exception ex)
            {
                // Ghi log hoặc xử lý lỗi tại đây
                throw new SecurityTokenException($"Token validation failed: {ex.Message}");
            }
        }


        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterModel registerModel)
        {
            if (_fakeUsers.Any(u => u.Username == registerModel.Username))
            {
                return BadRequest("User already exists.");
            }

            var newUser = new User
            {
                Username = registerModel.Username,
                Password = registerModel.Password,
                Roles = new List<string> { "User" }
            };

            _fakeUsers.Add(newUser);
            return Ok("User registered successfully.");
        }
    }
}
