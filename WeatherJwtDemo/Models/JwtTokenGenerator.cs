using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace WeatherJwtDemo.API.Models;

public class JwtTokenGenerator
{
    private IConfiguration _config;
    public JwtTokenGenerator(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateToken(string username, string password)
    {
        if (username != "lena" || password != "password")
        {
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not found")));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Name, username),
            new Claim(JwtRegisteredClaimNames.Email, "lena@gmail.com"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(120),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (bool IsValid, ClaimsPrincipal? Principal, string? Reason) ValidateToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _config["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _config["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = handler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            return (true, principal, "Token is valid");
        }
        catch (SecurityTokenExpiredException)
        {
            return (false, null, "Token has expired");
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            return (false, null, "Invalid token signature");
        }
        catch (SecurityTokenInvalidIssuerException)
        {
            return (false, null, "Invalid token issuer");
        }
        catch (SecurityTokenInvalidAudienceException)
        {
            return (false, null, "Invalid token audience");
        }
        catch (Exception ex)
        {
            return (false, null, $"Token validation failed: {ex.Message}");
        }
    }
}

