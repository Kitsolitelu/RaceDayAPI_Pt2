using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Helpers;
using RaceDayWeb.Models.Api;
using RaceDayWeb.Models.ViewModels;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApiClient _api;

        public HomeController(ApiClient api)
        {
            _api = api;
        }

        // home page: upcoming events with filters (organisers are sent to their dashboard)
        public async Task<IActionResult> Index(string? search, string? eventType, string? location)
        {
            var user = HttpContext.Session.GetUser();
            if (user != null && user.IsOrganiser)
            {
                return RedirectToAction("Dashboard", "Organiser");
            }

            var filters = new List<string>();
            if (!string.IsNullOrWhiteSpace(search))
            {
                filters.Add("search=" + Uri.EscapeDataString(search.Trim()));
            }
            if (!string.IsNullOrWhiteSpace(eventType))
            {
                filters.Add("eventType=" + Uri.EscapeDataString(eventType.Trim()));
            }
            if (!string.IsNullOrWhiteSpace(location))
            {
                filters.Add("location=" + Uri.EscapeDataString(location.Trim()));
            }

            var url = "api/events" + (filters.Count > 0 ? "?" + string.Join("&", filters) : "");
            var result = await _api.GetAsync<List<EventResponseDto>>(url);

            return View(new EventListViewModel
            {
                Events = result.Data ?? new List<EventResponseDto>(),
                Search = search,
                EventType = eventType,
                Location = location,
                Error = result.Success ? null : result.Error
            });
        }

        public IActionResult Error()
        {
            return View();
        }

        public IActionResult StatusPage(int id)
        {
            return View(id);
        }
    }
}
