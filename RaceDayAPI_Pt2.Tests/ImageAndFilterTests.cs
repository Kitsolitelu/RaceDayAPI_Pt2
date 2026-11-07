using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RaceDayApi.DTOs;
using Xunit;

namespace RaceDayApi.Tests
{
    // tests for the Part 3 additions: image uploads, event filters, organiser dashboard, results details
    public class ImageAndFilterTests : IClassFixture<RaceDayFactory>
    {
        private readonly RaceDayFactory _factory;

        public ImageAndFilterTests(RaceDayFactory factory)
        {
            _factory = factory;
        }

        // builds a small fake file to upload
        private static MultipartFormDataContent FileContent(string fileName, string contentType)
        {
            var file = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
            file.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            var form = new MultipartFormDataContent();
            form.Add(file, "file", fileName);
            return form;
        }

        [Fact]
        public async Task UploadBanner_AsOrganiser_Returns200_AndEventShowsBanner()
        {
            var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
            var ev = await TestHelpers.CreateEventAsync(organiser);

            var response = await organiser.PostAsync($"/api/events/{ev.EventId}/banner", FileContent("banner.png", "image/png"));
            var body = await response.Content.ReadFromJsonAsync<ImageUploadResponseDto>();
            var loaded = await organiser.GetFromJsonAsync<EventResponseDto>($"/api/events/{ev.EventId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("banners", body!.Url);
            Assert.Equal(body.Url, loaded!.BannerUrl);
        }

        [Fact]
        public async Task UploadBanner_NotAnImage_Returns400()
        {
            var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
            var ev = await TestHelpers.CreateEventAsync(organiser);

            var response = await organiser.PostAsync($"/api/events/{ev.EventId}/banner", FileContent("notes.txt", "text/plain"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UploadBanner_AsParticipant_Returns403()
        {
            var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
            var ev = await TestHelpers.CreateEventAsync(organiser);
            var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");

            var response = await participant.PostAsync($"/api/events/{ev.EventId}/banner", FileContent("banner.png", "image/png"));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UploadBanner_ByDifferentOrganiser_Returns403()
        {
            var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
            var ev = await TestHelpers.CreateEventAsync(organiser);
            var other = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");

            var response = await other.PostAsync($"/api/events/{ev.EventId}/banner", FileContent("banner.png", "image/png"));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UploadBanner_NotLoggedIn_Returns401()
        {
            var anonymous = TestHelpers.CreateTestClient(_factory);

            var response = await anonymous.PostAsync("/api/events/1/banner", FileContent("banner.png", "image/png"));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UploadProfilePicture_ThenProfileShowsPicture()
        {
            var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");

            var response = await participant.PostAsync("/api/users/me/picture", FileContent("me.jpg", "image/jpeg"));
            var body = await response.Content.ReadFromJsonAsync<ImageUploadResponseDto>();
            var profile = await participant.GetFromJsonAsync<UserProfileDto>("/api/users/me");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("profiles", body!.Url);
            Assert.Equal(body.Url, profile!.ProfilePictureUrl);
        }

        [Fact]
        public async Task MyEvents_AsOrganiser_ShowsOnlyOwnEventsWithEnrolmentCount()
        {
            var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
            var ev = await TestHelpers.CreateEventAsync(organiser);
            var category = await TestHelpers.CreateCategoryAsync(organiser, ev.EventId);
            var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
            await TestHelpers.EnrolAsync(participant, category.CategoryId);

            var other = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
            await TestHelpers.CreateEventAsync(other);

            var mine = await organiser.GetFromJsonAsync<List<EventResponseDto>>("/api/events/mine");

            Assert.Single(mine!);
            Assert.Equal(ev.EventId, mine![0].EventId);
            Assert.Equal(1, mine[0].EnrolmentCount);
        }

        [Fact]
        public async Task MyEvents_AsParticipant_Returns403()
        {
            var participant = await TestHelpers.LoggedInClientAsync(_factory, "Participant");

            var response = await participant.GetAsync("/api/events/mine");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetEvents_CanFilterByTypeAndName()
        {
            var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
            var token = Guid.NewGuid().ToString("N");

            var run = TestHelpers.SampleEvent();
            run.Name = "Filter run " + token;
            run.EventType = "Run";
            var cycle = TestHelpers.SampleEvent();
            cycle.Name = "Filter ride " + token;
            cycle.EventType = "Cycle";
            await organiser.PostAsJsonAsync("/api/events", run);
            await organiser.PostAsJsonAsync("/api/events", cycle);

            var anonymous = TestHelpers.CreateTestClient(_factory);
            var found = await anonymous.GetFromJsonAsync<List<EventResponseDto>>($"/api/events?search={token}&eventType=Cycle");

            Assert.Single(found!);
            Assert.Equal("Cycle", found![0].EventType);
        }

        [Fact]
        public async Task MyResults_IncludesEventNameAndTotalFinishers()
        {
            var organiser = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
            var ev = await TestHelpers.CreateEventAsync(organiser);
            var category = await TestHelpers.CreateCategoryAsync(organiser, ev.EventId);

            var first = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
            var second = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
            var firstEnrol = await (await TestHelpers.EnrolAsync(first, category.CategoryId)).Content.ReadFromJsonAsync<EnrolmentResponseDto>();
            var secondEnrol = await (await TestHelpers.EnrolAsync(second, category.CategoryId)).Content.ReadFromJsonAsync<EnrolmentResponseDto>();

            await organiser.PostAsJsonAsync($"/api/enrolments/{firstEnrol!.EnrolmentId}/results",
                new ResultDto { FinishTime = TimeSpan.FromMinutes(40), Position = 1 });
            await organiser.PostAsJsonAsync($"/api/enrolments/{secondEnrol!.EnrolmentId}/results",
                new ResultDto { FinishTime = TimeSpan.FromMinutes(50), Position = 2 });

            var mine = await second.GetFromJsonAsync<List<ResultResponseDto>>("/api/users/me/results");

            Assert.Single(mine!);
            Assert.Equal("Test Fun Run", mine![0].EventName);
            Assert.Equal(2, mine[0].Position);
            Assert.Equal(2, mine[0].TotalFinishers);
        }
    }
}
