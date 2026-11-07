using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.DTOs;
using RaceDayApi.Models;

namespace RaceDayApi.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly RaceDayContext _context;
        private readonly IConfiguration _config;

    
        private readonly PasswordHasher<User> _hasher = new PasswordHasher<User>();

        public AuthController(RaceDayContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        /// <summary>
        /// Creates a new account as an Organiser or a Participant.
        /// </summary
        /// <remarks>
        /// 
        /// <response code="201">Account created, returns the new user details.</response>
        /// <response code="400">Invalid details, duplicate email or wrong organiser code.</response>
        [HttpPost("register")]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            // only these two roles are allowed
            string role;
            if (dto.Role.Equals("Organiser", StringComparison.OrdinalIgnoreCase))
            {
                role = "Organiser";
            }
            else if (dto.Role.Equals("Participant", StringComparison.OrdinalIgnoreCase))
            {
                role = "Participant";
            }
            else
            {
                return BadRequest("Role must be either Organiser or Participant.");
            }

           
            if (role == "Organiser")
            {
                var secretCode = _config["OrganiserSettings:AccessCode"];
                if (string.IsNullOrEmpty(dto.OrganiserCode) || dto.OrganiserCode != secretCode)
                {
                    return BadRequest("Invalid organiser access code.");
                }
            }

           
            var email = dto.Email.Trim().ToLower();

            bool emailTaken = await _context.Users.AnyAsync(u => u.Email == email);
            if (emailTaken)
            {
                return BadRequest("An account with that email already exists.");
            }

            var user = new User
            {
                FullName = dto.FullName.Trim(),
                Email = email,
                Role = role
            };

    
            user.PasswordHash = _hasher.HashPassword(user, dto.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var result = new UserProfileDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role
            };

            return StatusCode(StatusCodes.Status201Created, result);
        }

    
        /// <response code="200">Logged in, returns the user's role.</response>
        /// <response code="401">Wrong email or password.</response>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var email = dto.Email.Trim().ToLower();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return Unauthorized("Invalid email or password.");
            }

         
            var check = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (check == PasswordVerificationResult.Failed)
            {
                return Unauthorized("Invalid email or password.");
            }

  
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return Ok(new { message = "Logged in successfully.", role = user.Role });
        }

        /// <response code="200">Logged out.</response>
        /// <response code="401">Not logged in.</response>
        [Authorize]
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new { message = "Logged out." });
        }
    }
}
