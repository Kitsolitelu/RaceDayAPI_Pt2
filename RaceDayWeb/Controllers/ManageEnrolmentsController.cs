using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Helpers;
using RaceDayWeb.Models.Api;
using RaceDayWeb.Models.ViewModels;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers
{
    // organisers see who has enrolled for one of their events
    [SessionAuthorize(Role = "Organiser")]
    public class ManageEnrolmentsController : Controller
    {
        private readonly ApiClient _api;

        public ManageEnrolmentsController(ApiClient api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index(int eventId)
        {
            var ev = await _api.GetAsync<EventResponseDto>($"api/events/{eventId}");
            if (!ev.Success || ev.Data == null)
            {
                return NotFound();
            }

            var enrolments = await _api.GetAsync<List<EnrolmentResponseDto>>($"api/events/{eventId}/enrolments");
            if (!enrolments.Success)
            {
                TempData["Error"] = enrolments.Error;
            }

            return View(new ManageEnrolmentsViewModel
            {
                Event = ev.Data,
                Enrolments = enrolments.Data ?? new List<EnrolmentResponseDto>()
            });
        }
    }
}
