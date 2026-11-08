using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Helpers;
using RaceDayWeb.Models;
using RaceDayWeb.Models.Api;
using RaceDayWeb.Models.ViewModels;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers
{
    // register, log in and log out. All the real work is done by the Part 2 API.
    public class AccountController : Controller
    {
        private readonly ApiClient _api;

        public AccountController(ApiClient api)
        {
            _api = api;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            var user = HttpContext.Session.GetUser();
            if (user != null)
            {
                return RedirectToHome(user);
            }

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var error = await SignInAsync(model.Email.Trim(), model.Password);
            if (error != null)
            {
                ModelState.AddModelError(string.Empty, error);
                return View(model);
            }

            var user = HttpContext.Session.GetUser()!;
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToHome(user);
        }

        [HttpGet]
        public IActionResult Register()
        {
            var user = HttpContext.Session.GetUser();
            if (user != null)
            {
                return RedirectToHome(user);
            }

            return View(new RegisterViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (model.Role == "Organiser" && string.IsNullOrWhiteSpace(model.OrganiserCode))
            {
                ModelState.AddModelError(nameof(model.OrganiserCode), "Organisers need the organiser access code.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var dto = new RegisterDto
            {
                FullName = model.FullName.Trim(),
                Email = model.Email.Trim(),
                Password = model.Password,
                Role = model.Role,
                OrganiserCode = model.Role == "Organiser" ? model.OrganiserCode?.Trim() : null
            };

            var result = await _api.PostAsync<object>("api/auth/register", dto);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error);
                return View(model);
            }

            // log them straight in so they do not have to type everything again
            var error = await SignInAsync(dto.Email, dto.Password);
            if (error != null)
            {
                TempData["Success"] = "Your account was created. Please log in.";
                return RedirectToAction(nameof(Login));
            }

            TempData["Success"] = "Welcome to RaceDay, " + dto.FullName + "!";
            return RedirectToHome(HttpContext.Session.GetUser()!);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _api.LogoutAsync();
            HttpContext.Session.Clear();

            TempData["Success"] = "You have been logged out.";
            return RedirectToAction(nameof(Login));
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        // logs in through the API, then keeps the user's name and role in the session
        private async Task<string?> SignInAsync(string email, string password)
        {
            var login = await _api.LoginAsync(email, password);
            if (!login.Success)
            {
                return login.Error;
            }

            var profile = await _api.GetAsync<UserProfileDto>("api/users/me");
            if (!profile.Success || profile.Data == null)
            {
                HttpContext.Session.Clear();
                return profile.Error.Length > 0 ? profile.Error : "We could not load your account. Please try again.";
            }

            HttpContext.Session.SetUser(new SessionUser
            {
                UserId = profile.Data.UserId,
                FullName = profile.Data.FullName,
                Email = profile.Data.Email,
                Role = profile.Data.Role,
                ProfilePictureUrl = profile.Data.ProfilePictureUrl
            });

            return null;
        }

        private IActionResult RedirectToHome(SessionUser user)
        {
            return user.IsOrganiser
                ? RedirectToAction("Dashboard", "Organiser")
                : RedirectToAction("Index", "Home");
        }
    }
}
