using System.Net;
using System.Net.Http.Json;
using RaceDayApi.DTOs;
using Xunit;

namespace RaceDayApi.Tests
{
   
    public class RoleAccessTests : IClassFixture<RaceDayFactory>
    {
        private readonly RaceDayFactory _factory;

        public RoleAccessTests(RaceDayFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CreateEvent_NotLoggedIn_Returns401()
        {
            var client = TestHelpers.CreateTestClient(_factory);

            var response = await client.PostAsJsonAsync("/api/events", TestHelpers.SampleEvent());

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateEvent_AsParticipant_Returns403()
        {
            var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");

            var response = await participant.PostAsJsonAsync("/api/events", TestHelpers.SampleEvent());

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UpdateEvent_AsParticipant_Returns403()
        {
            var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
            var created = await TestHelpers.CreateEventAsync(organiser);
            var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");

            var response = await participant.PutAsJsonAsync($"/api/events/{created.EventId}", TestHelpers.SampleEvent());

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DeleteEvent_AsParticipant_Returns403()
        {
            var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
            var created = await TestHelpers.CreateEventAsync(organiser);
            var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");

            var response = await participant.DeleteAsync($"/api/events/{created.EventId}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AddCategory_AsParticipant_Returns403()
        {
            var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
            var created = await TestHelpers.CreateEventAsync(organiser);
            var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");

            var dto = new CategoryDto { Name = "5km", Price = 50 };
            var response = await participant.PostAsJsonAsync($"/api/events/{created.EventId}/categories", dto);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Enrol_AsOrganiser_Returns403()
        {
            var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
            var created = await TestHelpers.CreateEventAsync(organiser);
            var category = await TestHelpers.CreateCategoryAsync(organiser, created.EventId);

            var response = await TestHelpers.EnrolAsync(organiser, category.CategoryId);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Enrol_NotLoggedIn_Returns401()
        {
            var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
            var created = await TestHelpers.CreateEventAsync(organiser);
            var category = await TestHelpers.CreateCategoryAsync(organiser, created.EventId);
            var anonymous = TestHelpers.CreateTestClient(_factory);

            var response = await TestHelpers.EnrolAsync(anonymous, category.CategoryId);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task MyEnrolments_AsOrganiser_Returns403()
        {
            var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");

            var response = await organiser.GetAsync("/api/users/me/enrolments");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task EventEnrolments_AsParticipant_Returns403()
        {
            var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
            var created = await TestHelpers.CreateEventAsync(organiser);
            var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");

            var response = await participant.GetAsync($"/api/events/{created.EventId}/enrolments");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CaptureResult_AsParticipant_Returns403()
        {
            var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");

            var result = new ResultDto { FinishTime = TimeSpan.FromMinutes(50), Position = 1 };
            var response = await participant.PostAsJsonAsync("/api/enrolments/1/results", result);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
