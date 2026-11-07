using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.DTOs;
using RaceDayApi.Models;

namespace RaceDayApi.Controllers
{
    [ApiController]
    [Route("api/enrolments")]
    public class EnrolmentsController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public EnrolmentsController(RaceDayContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }

      
        /// <response code="201">Result captured.</response>
        /// <response code="400">Invalid finish time or position.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Participant, or an event that belongs to another organiser.</response>
        /// <response code="404">No enrolment with that id.</response>
        /// <response code="409">This enrolment already has a result.</response>
        [Authorize(Roles = "Organiser")]
        [HttpPost("{id:int}/results")]
        [ProducesResponseType(typeof(ResultResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CaptureResult(int id, ResultDto dto)
        {
            if (dto.FinishTime <= TimeSpan.Zero)
            {
                return BadRequest("Finish time must be more than 00:00:00.");
            }

            var enrolment = await _context.Enrolments
                .Include(e => e.Category).ThenInclude(c => c.Event)
                .Include(e => e.Participant)
                .FirstOrDefaultAsync(e => e.EnrolmentId == id);

            if (enrolment == null)
            {
                return NotFound("Enrolment not found.");
            }

          
            if (enrolment.Category.Event.OrganiserId != GetCurrentUserId())
            {
                return Forbid();
            }

            bool hasResult = await _context.Results.AnyAsync(r => r.EnrolmentId == id);
            if (hasResult)
            {
                return Conflict("A result has already been captured for this enrolment.");
            }

            var result = new Result
            {
                EnrolmentId = id,
                FinishTime = dto.FinishTime,
                Position = dto.Position
            };

            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            var response = new ResultResponseDto
            {
                ResultId = result.ResultId,
                EnrolmentId = result.EnrolmentId,
                ParticipantName = enrolment.Participant.FullName,
                CategoryName = enrolment.Category.Name,
                FinishTime = result.FinishTime,
                Position = result.Position
            };

            return StatusCode(StatusCodes.Status201Created, response);
        }
    }
}
