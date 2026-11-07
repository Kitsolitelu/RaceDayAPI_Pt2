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

        
        /// <response code="201">
        /// <response code="401"
        /// <response code="403">
        /// <response code="404">
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

         
            bool alreadyEnrolled = await _context.Enrolments
                .AnyAsync(e => e.CategoryId == id && e.ParticipantId == userId);
            if (alreadyEnrolled)
            {
                return Conflict("You are already enrolled in this category.");
            }

        
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
