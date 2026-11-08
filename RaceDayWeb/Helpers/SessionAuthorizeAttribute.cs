using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDayWeb.Helpers
{
    // Put this on a controller or an action.
    // Not logged in -> sent to the login page. Wrong role -> sent to the access denied page.
    public class SessionAuthorizeAttribute : ActionFilterAttribute
    {
        public string? Role { get; set; }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var request = context.HttpContext.Request;
            var user = context.HttpContext.Session.GetUser();

            if (user == null)
            {
                string? returnUrl = request.Method == "GET" ? request.Path.Add(request.QueryString) : null;
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
                return;
            }

            if (Role != null && user.Role != Role)
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            }
        }
    }
}
