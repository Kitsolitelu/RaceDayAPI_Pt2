using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RaceDayApi.Data;
using RaceDayApi.DTOs;
using Xunit;

namespace RaceDayApi.Tests
{
    
    public class AuthTests : IClassFixture<RaceDayFactory>
    {
        private readonly RaceDayFactory _factory;

        public AuthTests(RaceDayFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Register_ValidParticipant_Returns201()
        {
            var client = TestHelpers.CreateTestClient(_factory);

            var response = await client.PostAsJsonAsync("/api/auth/register", TestHelpers.NewRegisterDto("Participant"));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<UserProfileDto>();
            Assert.Equal("Participant", body!.Role);
        }

        [Fact]
        public async Task Register_PasswordIsStoredHashed()
        {
            var client = TestHelpers.CreateTestClient(_factory);
            var dto = TestHelpers.NewRegisterDto("Participant");

            await client.PostAsJsonAsync("/api/auth/register", dto);

           
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RaceDayContext>();
            var user = db.Users.Single(u => u.Email == dto.Email);

            Assert.NotEqual(TestHelpers.Password, user.PasswordHash);
            Assert.True(user.PasswordHash.Length > 20);
        }

        [Fact]
        public async Task Register_DuplicateEmail_Returns400()
        {
            var client = TestHelpers.CreateTestClient(_factory);
            var dto = TestHelpers.NewRegisterDto("Participant");

            await client.PostAsJsonAsync("/api/auth/register", dto);
            var second = await client.PostAsJsonAsync("/api/auth/register", dto);

            Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        }

        [Fact]
        public async Task Register_OrganiserWithCorrectCode_Returns201()
        {
            var client = TestHelpers.CreateTestClient(_factory);

            var response = await client.PostAsJsonAsync("/api/auth/register", TestHelpers.NewRegisterDto("Organiser"));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task Register_OrganiserWithWrongCode_Returns400()
        {
            var client = TestHelpers.CreateTestClient(_factory);
            var dto = TestHelpers.NewRegisterDto("Organiser");
            dto.OrganiserCode = "WRONG-CODE";

            var response = await client.PostAsJsonAsync("/api/auth/register", dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Register_InvalidRole_Returns400()
        {
            var client = TestHelpers.CreateTestClient(_factory);
            var dto = TestHelpers.NewRegisterDto("Participant");
            dto.Role = "Admin";

            var response = await client.PostAsJsonAsync("/api/auth/register", dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Register_ShortPassword_Returns400()
        {
            var client = TestHelpers.CreateTestClient(_factory);
            var dto = TestHelpers.NewRegisterDto("Participant");
            dto.Password = "123";

            var response = await client.PostAsJsonAsync("/api/auth/register", dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Login_CorrectCredentials_Returns200()
        {
            var client = TestHelpers.CreateTestClient(_factory);
            var dto = TestHelpers.NewRegisterDto("Participant");
            await client.PostAsJsonAsync("/api/auth/register", dto);

            var login = new LoginDto { Email = dto.Email, Password = TestHelpers.Password };
            var response = await client.PostAsJsonAsync("/api/auth/login", login);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Login_WrongPassword_Returns401()
        {
            var client = TestHelpers.CreateTestClient(_factory);
            var dto = TestHelpers.NewRegisterDto("Participant");
            await client.PostAsJsonAsync("/api/auth/register", dto);

            var login = new LoginDto { Email = dto.Email, Password = "WrongPassword1" };
            var response = await client.PostAsJsonAsync("/api/auth/login", login);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_UnknownEmail_Returns401()
        {
            var client = TestHelpers.CreateTestClient(_factory);

            var login = new LoginDto { Email = TestHelpers.NewEmail(), Password = TestHelpers.Password };
            var response = await client.PostAsJsonAsync("/api/auth/login", login);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetProfile_LoggedIn_Returns200()
        {
            var client = await TestHelpers.LoggedInClientAsync(_factory, "Participant");

            var response = await client.GetAsync("/api/users/me");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<UserProfileDto>();
            Assert.Equal("Participant", body!.Role);
        }

        [Fact]
        public async Task GetProfile_NotLoggedIn_Returns401()
        {
            var client = TestHelpers.CreateTestClient(_factory);

            var response = await client.GetAsync("/api/users/me");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetProfile_AfterLogout_Returns401()
        {
            var client = await TestHelpers.LoggedInClientAsync(_factory, "Participant");

            await client.PostAsync("/api/auth/logout", null);
            var response = await client.GetAsync("/api/users/me");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
