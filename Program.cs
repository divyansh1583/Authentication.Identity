using Authentication.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapIdentityApi<IdentityUser>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();
