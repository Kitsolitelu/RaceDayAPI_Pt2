using System.Net;
using System.Net.Http.Json;
using RaceDayApi.DTOs;
using Xunit;

namespace RaceDayApi.Tests
{
    
    public class EventTests : IClassFixture<RaceDayFactory>
    {
        private readonly RaceDayFactory _factory;

        public EventTests(RaceDayFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetEvents_WithoutLogin_Returns200()
        {
            var client = TestHelpers.CreateTestClient(_factory);

            var response = await client.GetAsync("/api/events");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CreateEvent_AsOrganiser_Returns201()
        {
            var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");

            var response = await organiser.PostAsJsonAsync("/api/events", TestHelpers.SampleEvent());

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<EventResponseDto>();
            Assert.Equal("Test Fun Run", body!.Name);
            Assert.Equal("Run", body.EventType);
        }

        [Fact]
        public async Task CreateEvent_InvalidEventType_Returns400()
        {
            var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
            var dto = TestHelpers.SampleEvent();
            dto.EventType = "Swim";

            var response = await organiser.PostAsJsonAsync("/api/events", dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateEvent_AppearsInEventList()
        {
            var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
            var created = await TestHelpers.CreateEventAsync(organiser);

            
            var anonymous = TestHelpers.CreateTestClient(_factory);
            var events = await anonymous.GetFromJsonAsync<List<EventResponseDto>>("/api/events");

            Assert.Contains(events!, e => e.EventId == created.EventId);
        }

        [Fact]
        public async Task GetEvent_ReturnsDistanceFromRouteInfo()
        {
            var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
            var created = await TestHelpers.CreateEventAsync(organiser);

            var anonymous = TestHelpers.CreateTestClient(_factory);
            var ev = await anonymous.GetFromJsonAsync<EventResponseDto>($"/api/events/{created.EventId}");

            Assert.Equal(10, ev!.Distance);
        }

        [Fact]
        public async Task GetEvent_UnknownId_Returns404()
        {
            var client = TestHelpers.CreateTestClient(_factory);

            var response = await client.GetAsync("/api/events/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UpdateEvent_AsOrganiser_Returns200()
        {
            var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
            var created = await TestHelpers.CreateEventAsync(organiser);

            var update = TestHelpers.SampleEvent();
            update.Name = "Renamed Event";
            var response = await organiser.PutAsJsonAsync($"/api/events/{created.EventId}", update);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<EventResponseDto>();
            Assert.Equal("Renamed Event", body!.Name);
        }

        [Fact]
        public async Task UpdateEvent_ByDifferentOrganiser_Returns403()
        {
            var owner = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
            var created = await TestHelpers.CreateEventAsync(owner);
            var otherOrganiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");

            var response = await otherOrganiser.PutAsJsonAsync($"/api/events/{created.EventId}", TestHelpers.SampleEvent());

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DeleteEvent_AsOrganiser_Returns204_ThenEventIsGone()
        {
            var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
            var created = await TestHelpers.CreateEventAsync(organiser);

            var deleteResponse = await organiser.DeleteAsync($"/api/events/{created.EventId}");
            var getResponse = await organiser.GetAsync($"/api/events/{created.EventId}");

            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        [Fact]
        public async Task DeleteEvent_ByDifferentOrganiser_Returns403()
        {
            var owner = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
            var created = await TestHelpers.CreateEventAsync(owner);
            var otherOrganiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");

            var response = await otherOrganiser.DeleteAsync($"/api/events/{created.EventId}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AddCategory_AsOrganiser_Returns201_AndIsListed()
        {
            var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
            var created = await TestHelpers.CreateEventAsync(organiser);

            var category = await TestHelpers.CreateCategoryAsync(organiser, created.EventId);

            var anonymous = TestHelpers.CreateTestClient(_factory);
            var categories = await anonymous.GetFromJsonAsync<List<CategoryResponseDto>>($"/api/events/{created.EventId}/categories");
            Assert.Contains(categories!, c => c.CategoryId == category.CategoryId);
        }
    }
}
