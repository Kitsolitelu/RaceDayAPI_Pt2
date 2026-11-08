using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Helpers;
using RaceDayWeb.Models;
using RaceDayWeb.Models.Api;
using RaceDayWeb.Models.ViewModels;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers
{
    // view and update your own details, and upload a profile picture
    [SessionAuthorize]
    public class ProfileController : Controller
    {
        private readonly ApiClient _api;

        public ProfileController(ApiClient api)
        {
            _api = api;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var result = await _api.GetAsync<UserProfileDto>("api/users/me");
            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.Error;
                return RedirectToAction("Index", "Home");
            }

            return View(ToViewModel(result.Data));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ProfileViewModel model)
        {
            var current = HttpContext.Session.GetUser()!;
            model.Role = current.Role;
            model.ProfilePictureUrl = current.ProfilePictureUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var dto = new UpdateProfileDto { FullName = model.FullName.Trim(), Email = model.Email.Trim() };
            var result = await _api.PutAsync<UserProfileDto>("api/users/me", dto);

            if (!result.Success || result.Data == null)
            {
                ModelState.AddModelError(string.Empty, result.Error);
                return View(model);
            }

            SaveToSession(result.Data);
            TempData["Success"] = "Your details were updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPicture(IFormFile? picture)
        {
            if (picture == null || picture.Length == 0)
            {
                TempData["Error"] = "Please choose a picture first.";
                return RedirectToAction(nameof(Index));
            }

            var upload = await _api.UploadImageAsync("api/users/me/picture", picture);
            if (!upload.Success || upload.Data == null)
            {
                TempData["Error"] = upload.Error;
                return RedirectToAction(nameof(Index));
            }

            var user = HttpContext.Session.GetUser()!;
            user.ProfilePictureUrl = upload.Data.Url;
            HttpContext.Session.SetUser(user);

            TempData["Success"] = "Your profile picture was updated.";
            return RedirectToAction(nameof(Index));
        }

        private static ProfileViewModel ToViewModel(UserProfileDto dto)
        {
            return new ProfileViewModel
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Role = dto.Role,
                ProfilePictureUrl = dto.ProfilePictureUrl
            };
        }

        // keeps the name in the menu up to date
        private void SaveToSession(UserProfileDto dto)
        {
            HttpContext.Session.SetUser(new SessionUser
            {
                UserId = dto.UserId,
                FullName = dto.FullName,
                Email = dto.Email,
                Role = dto.Role,
                ProfilePictureUrl = dto.ProfilePictureUrl
            });
        }
    }
}
