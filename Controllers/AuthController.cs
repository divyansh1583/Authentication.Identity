using Authentication.Identity.Models;
using Authentication.Identity.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Authentication.Identity.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        ITokenService tokenService,
        IJwtTokenService jwtTokenService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorResponse(
                "Invalid request",
                ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                )
            ));
        }

        var user = new IdentityUser
        {
            UserName = request.Email,
            Email = request.Email
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new ErrorResponse(
                "Registration failed",
                new Dictionary<string, string[]>
                {
                    ["errors"] = result.Errors.Select(e => e.Description).ToArray()
                }
            ));
        }

        _logger.LogInformation("User {Email} registered successfully", request.Email);
        return Ok(new { message = "User registered successfully" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorResponse("Invalid request"));
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Unauthorized(new ErrorResponse("Invalid email or password"));
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            return Unauthorized(new ErrorResponse("Invalid email or password"));
        }

        // Generate tokens using Identity's BearerTokens
        var tokenResponse = await CreateTokenResponseAsync(user);

        _logger.LogInformation("User {Email} logged in successfully", request.Email);
        return Ok(tokenResponse);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            return BadRequest(new ErrorResponse("Refresh token is required"));
        }

        // In a production app, you would decode and validate the refresh token
        // For simplicity, this example uses Identity's token store
        
        // Note: This is a simplified implementation. In production, you should:
        // 1. Store refresh tokens securely in the database with expiration
        // 2. Validate the refresh token properly
        // 3. Implement token rotation

        return Unauthorized(new ErrorResponse("Invalid or expired refresh token"));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out successfully");
        return Ok(new { message = "Logged out successfully" });
    }

    private async Task<AuthResponse> CreateTokenResponseAsync(IdentityUser user)
    {
        // Generate refresh token
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync();
        await _tokenService.SaveRefreshTokenAsync(user.Id, refreshToken);

        // Generate JWT access token
        var accessToken = _jwtTokenService.GenerateAccessToken(user);

        return new AuthResponse(
            TokenType: "Bearer",
            AccessToken: accessToken,
            ExpiresIn: 3600, // 1 hour
            RefreshToken: refreshToken
        );
    }
}
