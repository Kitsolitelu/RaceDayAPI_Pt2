using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Helpers;
using RaceDayWeb.Models.Api;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers
{
    // pages that only a logged in Participant can open
    [SessionAuthorize(Role = "Participant")]
    public class ParticipantController : Controller
    {
        private readonly ApiClient _api;

        public ParticipantController(ApiClient api)
        {
            _api = api;
        }

        public async Task<IActionResult> MyEnrolments()
        {
            var result = await _api.GetAsync<List<EnrolmentResponseDto>>("api/users/me/enrolments");
            if (!result.Success)
            {
                TempData["Error"] = result.Error;
            }

            return View(result.Data ?? new List<EnrolmentResponseDto>());
        }

        public async Task<IActionResult> MyResults()
        {
            var result = await _api.GetAsync<List<ResultResponseDto>>("api/users/me/results");
            if (!result.Success)
            {
                TempData["Error"] = result.Error;
            }

            return View(result.Data ?? new List<ResultResponseDto>());
        }
    }
}
