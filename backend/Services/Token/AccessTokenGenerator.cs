using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

public class AccessTokenGenerator
{

    private readonly IConfiguration _configuration;

    public AccessTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public string GenerateToken(UserData user)
    {
        var secretKey = _configuration["Authentification:AccessTokenSecretKey"];
        SecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
        SigningCredentials credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        List<Claim> claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier,user.UserId.ToString()),
            new Claim(ClaimTypes.Name,user.UserName!),
            new Claim(ClaimTypes.Role, user.Role)
        };

        JwtSecurityToken token = new JwtSecurityToken(
            _configuration["Authentification:Issuer"],
            _configuration["Authentification:Audience"],
            claims,
            DateTime.UtcNow,
            DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Authentification:AccessTokenExpirationMinutes"])),
            credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}