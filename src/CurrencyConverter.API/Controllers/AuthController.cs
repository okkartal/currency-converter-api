using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CurrencyConverter.API.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CurrencyConverter.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthController> _logger;

    //In a real application, this would come from a database 
    private readonly Dictionary<string, UserCredentials> _users = new()
    {
        ["user1"] = new UserCredentials("password1", "User"),
        ["admin1"] = new UserCredentials("password1", "Admin")
    };

    public AuthController(IOptions<JwtSettings> jwtSettings, ILogger<AuthController> logger)
    {
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "Username and password is empty" });

        //Check if user exists
        if (!_users.TryGetValue(request.Username, out var userCredentials))
            return Unauthorized(new { error = "Invalid credentials" });

        //Validate password
        if (request.Password != userCredentials.Password) return Unauthorized(new { error = "Invalid credentials" });

        //Generate
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, request.Username),
            new(ClaimTypes.Role, userCredentials.Role),
            new("ClientId", Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryInMinutes),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);

        _logger.LogInformation($"Successfully authenticated user {request.Username}");

        return Ok(new
        {
            token = tokenHandler.WriteToken(token),
            expiration = tokenDescriptor.Expires,
            username = request.Username,
            role = userCredentials.Role
        });
    }

    public sealed record LoginRequest(string Username, string Password);

    private sealed record UserCredentials(string Password, string Role);
}