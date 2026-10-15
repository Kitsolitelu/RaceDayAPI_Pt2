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
    [Route("api/events")]
    public class EventsController : ControllerBase
    {
        private readonly RaceDayContext _context;

        // the only event types we allow
        private static readonly string[] ValidEventTypes = { "Run", "Walk", "Cycle" };

        public EventsController(RaceDayContext context)
        {
            _context = context;
        }

        // gets the id of the logged in user from the cookie
        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }

        // turns an Event (with its RouteInfo) into the object we send back
        private static EventResponseDto ToResponse(Event ev)
        {
            return new EventResponseDto
            {
                EventId = ev.EventId,
                OrganiserId = ev.OrganiserId,
                Name = ev.Name,
                Description = ev.Description,
                EventDate = ev.EventDate,
                Location = ev.Location,
                EventType = ev.EventType,
                Distance = ev.RouteInfo?.Distance,
                ElevationGainM = ev.RouteInfo?.ElevationGainM,
                RouteDescript = ev.RouteInfo?.RouteDescript
            };
        }

        /// <summary>
        /// Lists all upcoming events, soonest first.
        /// </summary>
        /// <remarks>Open to everyone, no login needed.</remarks>
        /// <response code="200">A list of upcoming events.</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<EventResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEvents()
        {
            var events = await _context.Events
                .Include(e => e.RouteInfo)
                .Where(e => e.EventDate >= DateTime.Today)
                .OrderBy(e => e.EventDate)
                .ToListAsync();

            return Ok(events.Select(ToResponse));
        }

        /// <summary>
        /// Returns the details of one event.
        /// </summary>
        /// <response code="200">The event.</response>
        /// <response code="404">No event with that id.</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEvent(int id)
        {
            var ev = await _context.Events
                .Include(e => e.RouteInfo)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (ev == null)
            {
                return NotFound();
            }

            return Ok(ToResponse(ev));
        }

        /// <summary>
        /// Creates a new event. Organisers only.
        /// </summary>
        /// <remarks>
        /// Send name, description, eventDate, location, eventType (Run, Walk or Cycle)
        /// and distance in km. The event belongs to the logged in organiser.
        /// </remarks>
        /// <response code="201">Event created.</response>
        /// <response code="400">Invalid details.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as a Participant.</response>
        [Authorize(Roles = "Organiser")]
        [HttpPost]
        [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateEvent(EventDto dto)
        {
            var eventType = ValidEventTypes.FirstOrDefault(t => t.Equals(dto.EventType, StringComparison.OrdinalIgnoreCase));
            if (eventType == null)
            {
                return BadRequest("Event type must be Run, Walk or Cycle.");
            }

            if (dto.EventDate == default)
            {
                return BadRequest("Please give a valid event date.");
            }

            var ev = new Event
            {
                OrganiserId = GetCurrentUserId(),
                Name = dto.Name.Trim(),
                Description = dto.Description,
                EventDate = dto.EventDate.Date,
                Location = dto.Location.Trim(),
                EventType = eventType,
                RouteInfo = new RouteInfo
                {
                    Distance = dto.Distance,
                    ElevationGainM = dto.ElevationGainM,
                    RouteDescript = dto.RouteDescript
                }
            };

            _context.Events.Add(ev);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEvent), new { id = ev.EventId }, ToResponse(ev));
        }

        /// <summary>
        /// Updates an event. Organisers only, and only their own events.
        /// </summary>
        /// <remarks>Send the same details as when creating an event.</remarks>
        /// <response code="200">Event updated.</response>
        /// <response code="400">Invalid details.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Participant, or an event that belongs to another organiser.</response>
        /// <response code="404">No event with that id.</response>
        [Authorize(Roles = "Organiser")]
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateEvent(int id, EventDto dto)
        {
            var eventType = ValidEventTypes.FirstOrDefault(t => t.Equals(dto.EventType, StringComparison.OrdinalIgnoreCase));
            if (eventType == null)
            {
                return BadRequest("Event type must be Run, Walk or Cycle.");
            }

            if (dto.EventDate == default)
            {
                return BadRequest("Please give a valid event date.");
            }

            var ev = await _context.Events
                .Include(e => e.RouteInfo)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (ev == null)
            {
                return NotFound();
            }

            // an organiser can only change their own events
            if (ev.OrganiserId != GetCurrentUserId())
            {
                return Forbid();
            }

            ev.Name = dto.Name.Trim();
            ev.Description = dto.Description;
            ev.EventDate = dto.EventDate.Date;
            ev.Location = dto.Location.Trim();
            ev.EventType = eventType;

            if (ev.RouteInfo == null)
            {
                ev.RouteInfo = new RouteInfo();
            }
            ev.RouteInfo.Distance = dto.Distance;
            ev.RouteInfo.ElevationGainM = dto.ElevationGainM;
            ev.RouteInfo.RouteDescript = dto.RouteDescript;

            await _context.SaveChangesAsync();

            return Ok(ToResponse(ev));
        }

        /// <summary>
        /// Deletes an event. Organisers only, and only their own events.
        /// </summary>
        /// <remarks>This also removes the event's categories, enrolments and results.</remarks>
        /// <response code="204">Event deleted.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Participant, or an event that belongs to another organiser.</response>
        /// <response code="404">No event with that id.</response>
        [Authorize(Roles = "Organiser")]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null)
            {
                return NotFound();
            }

            if (ev.OrganiserId != GetCurrentUserId())
            {
                return Forbid();
            }

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Lists the categories of one event.
        /// </summary>
        /// <remarks>Open to everyone, no login needed.</remarks>
        /// <response code="200">A list of categories.</response>
        /// <response code="404">No event with that id.</response>
        [HttpGet("{id:int}/categories")]
        [ProducesResponseType(typeof(List<CategoryResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCategories(int id)
        {
            bool eventExists = await _context.Events.AnyAsync(e => e.EventId == id);
            if (!eventExists)
            {
                return NotFound();
            }

            var categories = await _context.Categories
                .Where(c => c.EventId == id)
                .Select(c => new CategoryResponseDto
                {
                    CategoryId = c.CategoryId,
                    EventId = c.EventId,
                    Name = c.Name,
                    Price = c.Price,
                    MaxParticipants = c.MaxParticipants
                })
                .ToListAsync();

            return Ok(categories);
        }

        /// <summary>
        /// Adds a category to an event. Organisers only, and only for their own events.
        /// </summary>
        /// <remarks>Send name, price and (optionally) maxParticipants.</remarks>
        /// <response code="201">Category created.</response>
        /// <response code="400">Invalid details.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Participant, or an event that belongs to another organiser.</response>
        /// <response code="404">No event with that id.</response>
        [Authorize(Roles = "Organiser")]
        [HttpPost("{id:int}/categories")]
        [ProducesResponseType(typeof(CategoryResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddCategory(int id, CategoryDto dto)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null)
            {
                return NotFound();
            }

            if (ev.OrganiserId != GetCurrentUserId())
            {
                return Forbid();
            }

            var category = new Category
            {
                EventId = id,
                Name = dto.Name.Trim(),
                Price = dto.Price,
                MaxParticipants = dto.MaxParticipants
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            var response = new CategoryResponseDto
            {
                CategoryId = category.CategoryId,
                EventId = category.EventId,
                Name = category.Name,
                Price = category.Price,
                MaxParticipants = category.MaxParticipants
            };

            return StatusCode(StatusCodes.Status201Created, response);
        }

        /// <summary>
        /// Lists the captured results for an event, by finishing position.
        /// </summary>
        /// <remarks>Open to everyone, no login needed.</remarks>
        /// <response code="200">A list of results.</response>
        /// <response code="404">No event with that id.</response>
        [HttpGet("{id:int}/results")]
        [ProducesResponseType(typeof(List<ResultResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetResults(int id)
        {
            bool eventExists = await _context.Events.AnyAsync(e => e.EventId == id);
            if (!eventExists)
            {
                return NotFound();
            }

            var results = await _context.Results
                .Where(r => r.Enrolment.Category.EventId == id)
                .OrderBy(r => r.Position)
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

        /// <summary>
        /// Lists everyone enrolled in an event. Organisers only, and only for their own events.
        /// </summary>
        /// <response code="200">A list of enrolments for the event.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Participant, or an event that belongs to another organiser.</response>
        /// <response code="404">No event with that id.</response>
        [Authorize(Roles = "Organiser")]
        [HttpGet("{id:int}/enrolments")]
        [ProducesResponseType(typeof(List<EnrolmentResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEnrolments(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null)
            {
                return NotFound();
            }

            if (ev.OrganiserId != GetCurrentUserId())
            {
                return Forbid();
            }

            var enrolments = await _context.Enrolments
                .Where(e => e.Category.EventId == id)
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
    }
}
