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

app.MapIdentityApi<IdentityUser>();

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
