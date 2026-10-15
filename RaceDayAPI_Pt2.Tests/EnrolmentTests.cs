using System.Net;
using System.Net.Http.Json;
using RaceDayApi.DTOs;
using Xunit;

namespace RaceDayApi.Tests
{
    
    public class EnrolmentTests : IClassFixture<RaceDayFactory>
    {
        private readonly RaceDayFactory _factory;

        public EnrolmentTests(RaceDayFactory factory)
        {
            _factory = factory;
        }

       
        private async Task<(HttpClient Organiser, EventResponseDto Ev, CategoryResponseDto Category)> SetUpEventAsync(int? maxParticipants = null)
        {
            var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
            var ev = await TestHelpers.CreateEventAsync(organiser);
            var category = await TestHelpers.CreateCategoryAsync(organiser, ev.EventId, maxParticipants);
            return (organiser, ev, category);
        }

        [Fact]
        public async Task Enrol_AsParticipant_Returns201()
        {
            var setup = await SetUpEventAsync();
            var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");

            var response = await TestHelpers.EnrolAsync(participant, setup.Category.CategoryId);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task Enrol_IsRecorded_AppearsInMyEnrolments()
        {
            var setup = await SetUpEventAsync();
            var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
            await TestHelpers.EnrolAsync(participant, setup.Category.CategoryId);

            var mine = await participant.GetFromJsonAsync<List<EnrolmentResponseDto>>("/api/users/me/enrolments");

            Assert.Single(mine!);
            Assert.Equal(setup.Category.CategoryId, mine![0].CategoryId);
            Assert.Equal(setup.Ev.EventId, mine[0].EventId);
        }

        [Fact]
        public async Task Enrol_OrganiserCanSeeEnrolmentForTheirEvent()
        {
            var setup = await SetUpEventAsync();
            var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
            await TestHelpers.EnrolAsync(participant, setup.Category.CategoryId);

            var enrolments = await setup.Organiser.GetFromJsonAsync<List<EnrolmentResponseDto>>($"/api/events/{setup.Ev.EventId}/enrolments");

            Assert.Single(enrolments!);
            Assert.Equal(setup.Category.CategoryId, enrolments![0].CategoryId);
        }

        [Fact]
        public async Task Enrol_Twice_Returns409()
        {
            var setup = await SetUpEventAsync();
            var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");

            await TestHelpers.EnrolAsync(participant, setup.Category.CategoryId);
            var second = await TestHelpers.EnrolAsync(participant, setup.Category.CategoryId);

            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        public async Task Enrol_FullCategory_Returns409()
        {
            
            var setup = await SetUpEventAsync(maxParticipants: 1);
            var first = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
            var second = await TestHelpers.LoggedInClientAsync(_factory, "Participant");

            var firstResponse = await TestHelpers.EnrolAsync(first, setup.Category.CategoryId);
            var secondResponse = await TestHelpers.EnrolAsync(second, setup.Category.CategoryId);

            Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
        }

        [Fact]
        public async Task Enrol_UnknownCategory_Returns404()
        {
            var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");

            var response = await TestHelpers.EnrolAsync(participant, 999999);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CaptureResult_AsOrganiser_Returns201()
        {
            var setup = await SetUpEventAsync();
            var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
            var enrolResponse = await TestHelpers.EnrolAsync(participant, setup.Category.CategoryId);
            var enrolment = await enrolResponse.Content.ReadFromJsonAsync<EnrolmentResponseDto>();

            var result = new ResultDto { FinishTime = TimeSpan.FromMinutes(45), Position = 1 };
            var response = await setup.Organiser.PostAsJsonAsync($"/api/enrolments/{enrolment!.EnrolmentId}/results", result);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<ResultResponseDto>();
            Assert.Equal(1, body!.Position);
        }

        [Fact]
        public async Task CaptureResult_Twice_Returns409()
        {
            var setup = await SetUpEventAsync();
            var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
            var enrolResponse = await TestHelpers.EnrolAsync(participant, setup.Category.CategoryId);
            var enrolment = await enrolResponse.Content.ReadFromJsonAsync<EnrolmentResponseDto>();
            var result = new ResultDto { FinishTime = TimeSpan.FromMinutes(45), Position = 1 };
            var url = $"/api/enrolments/{enrolment!.EnrolmentId}/results";

            await setup.Organiser.PostAsJsonAsync(url, result);
            var second = await setup.Organiser.PostAsJsonAsync(url, result);

            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        public async Task CaptureResult_ByDifferentOrganiser_Returns403()
        {
            var setup = await SetUpEventAsync();
            var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
            var enrolResponse = await TestHelpers.EnrolAsync(participant, setup.Category.CategoryId);
            var enrolment = await enrolResponse.Content.ReadFromJsonAsync<EnrolmentResponseDto>();
            var otherOrganiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");

            var result = new ResultDto { FinishTime = TimeSpan.FromMinutes(45), Position = 1 };
            var response = await otherOrganiser.PostAsJsonAsync($"/api/enrolments/{enrolment!.EnrolmentId}/results", result);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task MyResults_ShowsCapturedResult()
        {
            var setup = await SetUpEventAsync();
            var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
            var enrolResponse = await TestHelpers.EnrolAsync(participant, setup.Category.CategoryId);
            var enrolment = await enrolResponse.Content.ReadFromJsonAsync<EnrolmentResponseDto>();

            var result = new ResultDto { FinishTime = TimeSpan.FromMinutes(52), Position = 3 };
            await setup.Organiser.PostAsJsonAsync($"/api/enrolments/{enrolment!.EnrolmentId}/results", result);

            var mine = await participant.GetFromJsonAsync<List<ResultResponseDto>>("/api/users/me/results");

            Assert.Single(mine!);
            Assert.Equal(3, mine![0].Position);
        }
    }
}
