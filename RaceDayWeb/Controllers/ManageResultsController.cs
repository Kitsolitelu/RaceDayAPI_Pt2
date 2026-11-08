using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Helpers;
using RaceDayWeb.Models.Api;
using RaceDayWeb.Models.ViewModels;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers
{
    // after the event, the organiser captures each participant's finish time and position
    [SessionAuthorize(Role = "Organiser")]
    public class ManageResultsController : Controller
    {
        private readonly ApiClient _api;

        public ManageResultsController(ApiClient api)
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
            var results = await _api.GetAsync<List<ResultResponseDto>>($"api/events/{eventId}/results");

            if (!enrolments.Success)
            {
                TempData["Error"] = enrolments.Error;
            }

            return View(new ManageResultsViewModel
            {
                Event = ev.Data,
                Enrolments = enrolments.Data ?? new List<EnrolmentResponseDto>(),
                Results = results.Data ?? new List<ResultResponseDto>()
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Capture(ResultFormViewModel model)
        {
            // finish time must look like 1:45:30 (hours:minutes:seconds)
            if (!TimeSpan.TryParseExact(model.FinishTime?.Trim(), new[] { @"h\:mm\:ss", @"hh\:mm\:ss" },
                    CultureInfo.InvariantCulture, out var finishTime) || finishTime <= TimeSpan.Zero)
            {
                TempData["Error"] = "Please enter the finish time as hours:minutes:seconds, for example 0:52:30.";
                return RedirectToAction(nameof(Index), new { eventId = model.EventId });
            }

            if (model.Position < 1)
            {
                TempData["Error"] = "The finishing position must be 1 or higher.";
                return RedirectToAction(nameof(Index), new { eventId = model.EventId });
            }

            var dto = new ResultDto { FinishTime = finishTime, Position = model.Position };
            var result = await _api.PostAsync<ResultResponseDto>($"api/enrolments/{model.EnrolmentId}/results", dto);

            if (result.Success)
            {
                TempData["Success"] = "The result was saved.";
            }
            else
            {
                TempData["Error"] = result.Error;
            }

            return RedirectToAction(nameof(Index), new { eventId = model.EventId });
        }
    }
}
