using Authentication.Identity;
using Authentication.Identity.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

IServiceCollection serviceCollection = builder.Services;
ServiceLifetime contextLifetime = ServiceLifetime.Scoped;
ServiceLifetime optionsLifetime = ServiceLifetime.Scoped;

string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    
Action<DbContextOptionsBuilder> optionsAction = (DbContextOptionsBuilder options) =>
{
    options.UseSqlServer(connectionString);
};
// Add services to the container.

builder.Services.AddDbContext<AuthDbContext>(optionsAction, contextLifetime, optionsLifetime);

builder.Services.AddAuthorization();

builder.Services.AddIdentityApiEndpoints<IdentityUser>()
    .AddEntityFrameworkStores<AuthDbContext>();

// Add custom services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? 
            throw new InvalidOperationException("JWT Key not configured")))
    };
});

// Add controllers
builder.Services.AddControllers();

builder.Services.AddOpenApi();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

//Kestral enforces this even if you dont use this in development
//because its i think in vs settings does we need to it for production
//there settings might be different and we dont want to guess.

//In development visual studio uses launch.json to confif kestral server
//but that file is ignored in production so thats why this middleware needs to be
//used even though redirection works without in development
app.UseHttpsRedirection();

app.UseCors("AllowAngular");

app.UseAuthentication();
app.UseAuthorization();

// Map custom controllers
app.MapControllers();

// Keep Identity API endpoints if you want to use them alongside custom endpoints
// Comment out the line below if you only want custom endpoints
// app.MapIdentityApi<IdentityUser>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
}

app.UseHttpsRedirection();

app.Run();
