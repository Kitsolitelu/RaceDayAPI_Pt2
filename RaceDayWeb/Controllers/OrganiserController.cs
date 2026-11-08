using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Helpers;
using RaceDayWeb.Models.Api;
using RaceDayWeb.Models.ViewModels;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers
{
    // the organiser's dashboard: all their events, enrolments per event and upcoming dates
    [SessionAuthorize(Role = "Organiser")]
    public class OrganiserController : Controller
    {
        private readonly ApiClient _api;

        public OrganiserController(ApiClient api)
        {
            _api = api;
        }

        public async Task<IActionResult> Dashboard()
        {
            var result = await _api.GetAsync<List<EventResponseDto>>("api/events/mine");

            return View(new DashboardViewModel
            {
                Events = result.Data ?? new List<EventResponseDto>(),
                Error = result.Success ? null : result.Error
            });
        }
    }
}
