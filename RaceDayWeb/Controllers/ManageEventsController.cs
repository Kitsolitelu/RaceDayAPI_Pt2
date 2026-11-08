using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Helpers;
using RaceDayWeb.Models.Api;
using RaceDayWeb.Models.ViewModels;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers
{
    // organisers create, edit and delete their events here (including the banner image)
    [SessionAuthorize(Role = "Organiser")]
    public class ManageEventsController : Controller
    {
        private readonly ApiClient _api;

        public ManageEventsController(ApiClient api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index()
        {
            var result = await _api.GetAsync<List<EventResponseDto>>("api/events/mine");
            if (!result.Success)
            {
                TempData["Error"] = result.Error;
            }

            return View(result.Data ?? new List<EventResponseDto>());
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new EventFormViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var created = await _api.PostAsync<EventResponseDto>("api/events", ToDto(model));
            if (!created.Success || created.Data == null)
            {
                ModelState.AddModelError(string.Empty, created.Error);
                return View(model);
            }

            // the banner is sent to the API after the event exists, because it needs the event id
            if (model.Banner != null && model.Banner.Length > 0)
            {
                var upload = await _api.UploadImageAsync($"api/events/{created.Data.EventId}/banner", model.Banner);
                if (!upload.Success)
                {
                    TempData["Error"] = "The event was created, but the banner could not be saved: " + upload.Error;
                    return RedirectToAction(nameof(Edit), new { id = created.Data.EventId });
                }
            }

            TempData["Success"] = "The event was created. Now add categories so people can enrol.";
            return RedirectToAction("Index", "ManageCategories", new { eventId = created.Data.EventId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var result = await _api.GetAsync<EventResponseDto>($"api/events/{id}");
            if (!result.Success || result.Data == null)
            {
                return NotFound();
            }

            var ev = result.Data;
            return View(new EventFormViewModel
            {
                EventId = ev.EventId,
                Name = ev.Name,
                Description = ev.Description,
                EventDate = ev.EventDate,
                Location = ev.Location,
                EventType = ev.EventType,
                Distance = ev.Distance ?? 0,
                ElevationGainM = ev.ElevationGainM,
                RouteDescript = ev.RouteDescript,
                CurrentBannerUrl = ev.BannerUrl
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EventFormViewModel model)
        {
            model.EventId = id;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var updated = await _api.PutAsync<EventResponseDto>($"api/events/{id}", ToDto(model));
            if (!updated.Success || updated.Data == null)
            {
                ModelState.AddModelError(string.Empty, updated.Error);
                return View(model);
            }

            if (model.Banner != null && model.Banner.Length > 0)
            {
                var upload = await _api.UploadImageAsync($"api/events/{id}/banner", model.Banner);
                if (!upload.Success)
                {
                    TempData["Error"] = "The event was saved, but the banner could not be uploaded: " + upload.Error;
                    return RedirectToAction(nameof(Edit), new { id });
                }
            }

            TempData["Success"] = "The event was updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _api.GetAsync<EventResponseDto>($"api/events/{id}");
            if (!result.Success || result.Data == null)
            {
                return NotFound();
            }

            return View(result.Data);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _api.DeleteAsync($"api/events/{id}");

            if (result.Success)
            {
                TempData["Success"] = "The event was deleted.";
            }
            else
            {
                TempData["Error"] = result.Error;
            }

            return RedirectToAction(nameof(Index));
        }

        private static EventDto ToDto(EventFormViewModel model)
        {
            return new EventDto
            {
                Name = model.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
                EventDate = model.EventDate,
                Location = model.Location.Trim(),
                EventType = model.EventType,
                Distance = model.Distance,
                ElevationGainM = model.ElevationGainM,
                RouteDescript = string.IsNullOrWhiteSpace(model.RouteDescript) ? null : model.RouteDescript.Trim()
            };
        }
    }
}
