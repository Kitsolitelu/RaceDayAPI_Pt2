using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// connects the DbContext to SQL Server using the connection string in appsettings.json
builder.Services.AddDbContext<RaceDayContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RaceDayConnection")));

// cookie authentication - the cookie is our session, it remembers who is logged in and their role
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "RaceDay.Session";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;

        // for an API we want a status code, not a redirect to a login page
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

// Azure Blob Storage for banner and profile images, the connection string is in appsettings.json
builder.Services.AddSingleton<IBlobStorageService>(_ =>
    new BlobStorageService(
        builder.Configuration["BlobStorage:ConnectionString"] ?? "UseDevelopmentStorage=true",
        builder.Configuration["BlobStorage:ContainerName"] ?? "raceday-images"));

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Swagger page that reads the OpenAPI document above - open /swagger in the browser
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "RaceDay API v1");
    });
}

app.UseHttpsRedirection();

// authentication must come before authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// needed so the test project can start the API (top level statements hide the Program class)
public partial class Program { }
