using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.DTOs;
using RaceDayApi.Services;

namespace RaceDayApi.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize] // every endpoint here needs a logged in user, Organiser or Participant
    public class UsersController : ControllerBase
    {
        private readonly RaceDayContext _context;
        private readonly IBlobStorageService _blobStorage;

        public UsersController(RaceDayContext context, IBlobStorageService blobStorage)
        {
            _context = context;
            _blobStorage = blobStorage;
        }

        // gets the id of the logged in user from the cookie
        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }

        /// <summary>
        /// Returns the profile of the logged in user.
        /// </summary>
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
                Role = user.Role,
                ProfilePictureUrl = user.ProfilePictureUrl
            });
        }

        /// <summary>
        /// Updates the full name and email of the logged in user.
        /// </summary>
        /// <remarks>
        /// Send fullName and email. A user can only update their own profile.
        /// </remarks>
        /// <response code="200">Returns the updated details.</response>
        /// <response code="400">Invalid details or the email belongs to another account.</response>
        /// <response code="401">Not logged in.</response>
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

            // make sure nobody else already uses this email
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
                Role = user.Role,
                ProfilePictureUrl = user.ProfilePictureUrl
            });
        }

        /// <summary>
        /// Uploads a profile picture for the logged in user to Blob Storage.
        /// </summary>
        /// <remarks>Send the image as a form file called "file". JPG, PNG or WEBP, 5 MB at most.</remarks>
        /// <response code="200">Picture saved, returns the image link.</response>
        /// <response code="400">Not an image, or the image is too big.</response>
        /// <response code="401">Not logged in.</response>
        [HttpPost("me/picture")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ImageUploadResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UploadMyPicture(IFormFile file)
        {
            var error = ImageValidator.Validate(file);
            if (error != null)
            {
                return BadRequest(error);
            }

            var user = await _context.Users.FindAsync(GetCurrentUserId());
            if (user == null)
            {
                return NotFound();
            }

            user.ProfilePictureUrl = await _blobStorage.UploadImageAsync(file, "profiles");
            await _context.SaveChangesAsync();

            return Ok(new ImageUploadResponseDto { Url = user.ProfilePictureUrl });
        }

        /// <summary>
        /// Lists the logged in participant's own enrolments. Participants only.
        /// </summary>
        /// <response code="200">A list of the participant's enrolments.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as an Organiser.</response>
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

        /// <summary>
        /// Lists the logged in participant's own results. Participants only.
        /// </summary>
        /// <response code="200">A list of the participant's results.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as an Organiser.</response>
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
                    Position = r.Position,
                    EventName = r.Enrolment.Category.Event.Name,
                    EventDate = r.Enrolment.Category.Event.EventDate,
                    TotalFinishers = _context.Results.Count(x =>
                        x.Enrolment.Category.EventId == r.Enrolment.Category.EventId)
                })
                .OrderByDescending(r => r.EventDate)
                .ToListAsync();

            return Ok(results);
        }
    }
}
