using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.DTOs;

namespace RaceDayApi.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize] 
    public class UsersController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public UsersController(RaceDayContext context)
        {
            _context = context;
        }

        
        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }

        /// <response code="200">The user's own details.</response>
        /// <response code="401">Not logged in.</response>
        [HttpGet("me")]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyProfile()
        {
            var user = await _context.Users.FindAsync(GetCurrentUserId());
            if (user == null)
            {
                return NotFound();
            }

            return Ok(new UserProfileDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role
            });
        }

        /// <response code="200">
        /// <response code="400">
        /// <response code="401">
        [HttpPut("me")]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateMyProfile(UpdateProfileDto dto)
        {
            var userId = GetCurrentUserId();

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var email = dto.Email.Trim().ToLower();

         
            bool emailUsed = await _context.Users.AnyAsync(u => u.Email == email && u.UserId != userId);
            if (emailUsed)
            {
                return BadRequest("That email is already used by another account.");
            }

            user.FullName = dto.FullName.Trim();
            user.Email = email;
            await _context.SaveChangesAsync();

            return Ok(new UserProfileDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role
            });
        }

        /// <response code="200"
        /// <response code="401"
        /// <response code="403"
        [Authorize(Roles = "Participant")]
        [HttpGet("me/enrolments")]
        [ProducesResponseType(typeof(List<EnrolmentResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMyEnrolments()
        {
            var userId = GetCurrentUserId();

            var enrolments = await _context.Enrolments
                .Where(e => e.ParticipantId == userId)
                .Select(e => new EnrolmentResponseDto
                {
                    EnrolmentId = e.EnrolmentId,
                    CategoryId = e.CategoryId,
                    CategoryName = e.Category.Name,
                    EventId = e.Category.EventId,
                    EventName = e.Category.Event.Name,
                    ParticipantId = e.ParticipantId,
                    ParticipantName = e.Participant.FullName,
                    EnrolmentDate = e.EnrolmentDate,
                    Status = e.Status
                })
                .ToListAsync();

            return Ok(enrolments);
        }

      
        /// <response code="200"
        /// <response code="401">
        /// <response code="403">
        [Authorize(Roles = "Participant")]
        [HttpGet("me/results")]
        [ProducesResponseType(typeof(List<ResultResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMyResults()
        {
            var userId = GetCurrentUserId();

            var results = await _context.Results
                .Where(r => r.Enrolment.ParticipantId == userId)
                .Select(r => new ResultResponseDto
                {
                    ResultId = r.ResultId,
                    EnrolmentId = r.EnrolmentId,
                    ParticipantName = r.Enrolment.Participant.FullName,
                    CategoryName = r.Enrolment.Category.Name,
                    FinishTime = r.FinishTime,
                    Position = r.Position
                })
                .ToListAsync();

            return Ok(results);
        }
    }
}
