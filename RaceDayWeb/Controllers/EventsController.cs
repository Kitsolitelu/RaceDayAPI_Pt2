using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Helpers;
using RaceDayWeb.Models.Api;
using RaceDayWeb.Models.ViewModels;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers
{
    // event details page and enrolling (participants only)
    public class EventsController : Controller
    {
        private readonly ApiClient _api;

        public EventsController(ApiClient api)
        {
            _api = api;
        }

        public async Task<IActionResult> Details(int id)
        {
            var ev = await _api.GetAsync<EventResponseDto>($"api/events/{id}");
            if (!ev.Success || ev.Data == null)
            {
                return NotFound();
            }

            var categories = await _api.GetAsync<List<CategoryResponseDto>>($"api/events/{id}/categories");

            return View(new EventDetailsViewModel
            {
                Event = ev.Data,
                Categories = categories.Data ?? new List<CategoryResponseDto>()
            });
        }

        [SessionAuthorize(Role = "Participant")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Enrol(int eventId, int categoryId)
        {
            var result = await _api.PostAsync<EnrolmentResponseDto>($"api/categories/{categoryId}/enrol", null);

            if (!result.Success)
            {
                TempData["Error"] = result.Error;
                return RedirectToAction(nameof(Details), new { id = eventId });
            }

            TempData["Success"] = "You are enrolled! Your enrolment is shown below.";
            return RedirectToAction("MyEnrolments", "Participant");
        }
    }
}
