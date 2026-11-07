using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDayApi.Data;
using RaceDayApi.DTOs;
using RaceDayApi.Services;

namespace RaceDayApi.Tests
{
    // starts the real API in memory for the tests, but swaps SQL Server for an in-memory database
    public class RaceDayFactory : WebApplicationFactory<Program>
    {
        // every factory gets its own database name so test classes don't share data
        private readonly string _dbName = "RaceDayTestDb_" + Guid.NewGuid();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("OrganiserSettings:AccessCode", TestHelpers.OrganiserCode);

            builder.ConfigureServices(services =>
            {
                // EF Core keeps the SQL Server setup in more than one registration,
                // so we remove all of them before adding the in-memory database
                var oldSetup = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<RaceDayContext>) ||
                    (d.ServiceType.IsGenericType &&
                     d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration") &&
                     d.ServiceType.GenericTypeArguments.Contains(typeof(RaceDayContext))))
                    .ToList();

                foreach (var item in oldSetup)
                {
                    services.Remove(item);
                }

                services.AddDbContext<RaceDayContext>(options =>
                    options.UseInMemoryDatabase(_dbName));

                // never talk to Azure in the tests, use a fake that just returns a link
                services.RemoveAll<IBlobStorageService>();
                services.AddSingleton<IBlobStorageService, FakeBlobStorageService>();
            });
        }
    }

    // stands in for Azure Blob Storage during the tests
    public class FakeBlobStorageService : IBlobStorageService
    {
        public Task<string> UploadImageAsync(IFormFile file, string folder)
        {
            return Task.FromResult($"https://fake-storage.test/{folder}/{file.FileName}");
        }
    }

    // small helper methods so the tests stay short and easy to read
    public static class TestHelpers
    {
        public const string OrganiserCode = "RACEDAY-ORG-2026";
        public const string Password = "Password123";

        // a new client has its own cookies, so each client is its own "browser"
        public static HttpClient CreateTestClient(RaceDayFactory factory)
        {
            return factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost")
            });
        }

        // random email so tests never clash with each other
        public static string NewEmail()
        {
            return $"user_{Guid.NewGuid():N}@test.com";
        }

        public static RegisterDto NewRegisterDto(string role)
        {
            return new RegisterDto
            {
                FullName = "Test " + role,
                Email = NewEmail(),
                Password = Password,
                Role = role,
                OrganiserCode = role == "Organiser" ? OrganiserCode : null
            };
        }

        // registers a new user and logs them in, the client keeps the session cookie
        public static async Task<HttpClient> LoggedInClientAsync(RaceDayFactory factory, string role)
        {
            var client = CreateTestClient(factory);
            var register = NewRegisterDto(role);

            var registerResponse = await client.PostAsJsonAsync("/api/auth/register", register);
            registerResponse.EnsureSuccessStatusCode();

            var login = new LoginDto { Email = register.Email, Password = Password };
            var loginResponse = await client.PostAsJsonAsync("/api/auth/login", login);
            loginResponse.EnsureSuccessStatusCode();

            return client;
        }

        public static EventDto SampleEvent()
        {
            return new EventDto
            {
                Name = "Test Fun Run",
                Description = "A run used in the tests",
                EventDate = DateTime.Today.AddDays(30),
                Location = "Pretoria",
                EventType = "Run",
                Distance = 10
            };
        }

        public static async Task<EventResponseDto> CreateEventAsync(HttpClient organiser)
        {
            var response = await organiser.PostAsJsonAsync("/api/events", SampleEvent());
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<EventResponseDto>())!;
        }

        public static async Task<CategoryResponseDto> CreateCategoryAsync(HttpClient organiser, int eventId, int? maxParticipants = null)
        {
            var dto = new CategoryDto { Name = "10km", Price = 150, MaxParticipants = maxParticipants };
            var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", dto);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<CategoryResponseDto>())!;
        }

        public static Task<HttpResponseMessage> EnrolAsync(HttpClient participant, int categoryId)
        {
            return participant.PostAsync($"/api/categories/{categoryId}/enrol", null);
        }
    }
}
