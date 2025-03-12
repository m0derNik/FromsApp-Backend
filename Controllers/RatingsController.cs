using FormsApp.Api.Data;
using FormsApp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace FormsApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RatingsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RatingsController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/ratings
        [HttpPost]
        public async Task<ActionResult<Rating>> CreateRating([FromBody] CreateRatingRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var template = await _context.Templates.FindAsync(request.TemplateId);
            if (template == null)
                return NotFound("Template not found");

            var userId = GetCurrentUserId();
            var existingRating = await _context.Ratings
                .FirstOrDefaultAsync(r => r.TemplateId == request.TemplateId && r.UserId == userId);

            if (existingRating != null)
            {
                existingRating.Value = request.Value;
            }
            else
            {
                var rating = new Rating
                {
                    TemplateId = request.TemplateId,
                    UserId = userId,
                    Value = request.Value
                };
                _context.Ratings.Add(rating);
            }

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Rating submitted" });
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim ?? throw new UnauthorizedAccessException());
        }
    }

    public class CreateRatingRequest
    {
        [Required(ErrorMessage = "TemplateId is required")]
        public int TemplateId { get; set; }

        [Required(ErrorMessage = "Value is required")]
        [Range(1, 5, ErrorMessage = "Rating value must be between 1 and 5")]
        public int Value { get; set; }
    }
}