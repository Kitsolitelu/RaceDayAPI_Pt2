using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Helpers;
using RaceDayWeb.Models.Api;
using RaceDayWeb.Models.ViewModels;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers
{
    // organisers add age or distance categories to an event
    [SessionAuthorize(Role = "Organiser")]
    public class ManageCategoriesController : Controller
    {
        private readonly ApiClient _api;

        public ManageCategoriesController(ApiClient api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index(int eventId)
        {
            var model = await LoadAsync(eventId);
            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Add([Bind(Prefix = "NewCategory")] CategoryFormViewModel form)
        {
            if (!ModelState.IsValid)
            {
                var model = await LoadAsync(form.EventId);
                if (model == null)
                {
                    return NotFound();
                }

                model.NewCategory = form;
                return View(nameof(Index), model);
            }

            var dto = new CategoryDto { Name = form.Name.Trim(), Price = form.Price, MaxParticipants = form.MaxParticipants };
            var result = await _api.PostAsync<CategoryResponseDto>($"api/events/{form.EventId}/categories", dto);

            if (result.Success)
            {
                TempData["Success"] = "The category was added.";
            }
            else
            {
                TempData["Error"] = result.Error;
            }

            return RedirectToAction(nameof(Index), new { eventId = form.EventId });
        }

        private async Task<ManageCategoriesViewModel?> LoadAsync(int eventId)
        {
            var ev = await _api.GetAsync<EventResponseDto>($"api/events/{eventId}");
            if (!ev.Success || ev.Data == null)
            {
                return null;
            }

            var categories = await _api.GetAsync<List<CategoryResponseDto>>($"api/events/{eventId}/categories");

            return new ManageCategoriesViewModel
            {
                Event = ev.Data,
                Categories = categories.Data ?? new List<CategoryResponseDto>(),
                NewCategory = new CategoryFormViewModel { EventId = eventId }
            };
        }
    }
}
