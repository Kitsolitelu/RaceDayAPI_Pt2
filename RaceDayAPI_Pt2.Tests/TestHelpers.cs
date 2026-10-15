using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDayApi.Data;
using RaceDayApi.DTOs;

namespace RaceDayApi.Tests
{
    public class RaceDayFactory : WebApplicationFactory<Program>
    {
       
        private readonly string _dbName = "RaceDayTestDb_" + Guid.NewGuid();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("OrganiserSettings:AccessCode", TestHelpers.OrganiserCode);

            builder.ConfigureServices(services =>
            {
                
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
            });
        }
    }

 
    public static class TestHelpers
    {
        public const string OrganiserCode = "RACEDAY-ORG-2026";
        public const string Password = "Password123";

       
        public static HttpClient CreateTestClient(RaceDayFactory factory)
        {
            return factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost")
            });
        }

       
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
