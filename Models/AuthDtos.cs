namespace Authentication.Identity.Models;

public record RegisterRequest(
    string Email,
    string Password
);

public record LoginRequest(
    string Email,
    string Password
);

public record AuthResponse(
    string TokenType,
    string AccessToken,
    int ExpiresIn,
    string RefreshToken
);

public record RefreshTokenRequest(
    string RefreshToken
);

public record ErrorResponse(
    string Message,
    Dictionary<string, string[]>? Errors = null
);
