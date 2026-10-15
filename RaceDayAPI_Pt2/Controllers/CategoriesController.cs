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
    [Route("api/categories")]
    public class CategoriesController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public CategoriesController(RaceDayContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }

        /// <summary>
        /// Enrols the logged in participant into a category. Participants only.
        /// </summary>
        /// <remarks>No body needed, the category id is in the address.</remarks>
        /// <response code="201">Enrolment created.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as an Organiser.</response>
        /// <response code="404">No category with that id.</response>
        /// <response code="409">Already enrolled in this category, or the category is full.</response>
        [Authorize(Roles = "Participant")]
        [HttpPost("{id:int}/enrol")]
        [ProducesResponseType(typeof(EnrolmentResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Enrol(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound("Category not found.");
            }

            var userId = GetCurrentUserId();

            // one enrolment per participant per category
            bool alreadyEnrolled = await _context.Enrolments
                .AnyAsync(e => e.CategoryId == id && e.ParticipantId == userId);
            if (alreadyEnrolled)
            {
                return Conflict("You are already enrolled in this category.");
            }

            // check the category is not full
            if (category.MaxParticipants != null)
            {
                int taken = await _context.Enrolments.CountAsync(e => e.CategoryId == id);
                if (taken >= category.MaxParticipants)
                {
                    return Conflict("This category is full.");
                }
            }

            var enrolment = new Enrolment
            {
                CategoryId = id,
                ParticipantId = userId
            };

            _context.Enrolments.Add(enrolment);
            await _context.SaveChangesAsync();

            var response = new EnrolmentResponseDto
            {
                EnrolmentId = enrolment.EnrolmentId,
                CategoryId = category.CategoryId,
                CategoryName = category.Name,
                EventId = category.EventId,
                EventName = category.Event.Name,
                ParticipantId = userId,
                ParticipantName = User.FindFirst(ClaimTypes.Name)!.Value,
                EnrolmentDate = enrolment.EnrolmentDate,
                Status = enrolment.Status
            };

            return StatusCode(StatusCodes.Status201Created, response);
        }
    }
}
