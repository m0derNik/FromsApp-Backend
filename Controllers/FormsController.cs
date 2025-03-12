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
    public class FormsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FormsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/forms
        [HttpGet]
        public async Task<ActionResult<PagedResponse<FormDto>>> GetForms(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10)
        {
            page = Math.Max(1, page); // Убеждаемся, что page >= 1
            var userId = GetCurrentUserId();
            var query = _context.Forms.Where(f => f.UserId == userId);
            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new FormDto
                {
                    Id = f.Id,
                    TemplateId = f.TemplateId,
                    UserId = f.UserId,
                    CreatedAt = f.CreatedAt,
                    Answers = f.Answers.Select(a => new AnswerDto
                    {
                        Id = a.Id,
                        QuestionId = a.QuestionId,
                        Value = a.Value
                    }).ToList()
                })
                .ToListAsync();

            return new PagedResponse<FormDto>(items, totalCount, page, pageSize);
        }

        // GET: api/forms/5
        [HttpGet("{id}")]
        public async Task<ActionResult<FormDto>> GetForm(int id)
        {
            var form = await _context.Forms
                .Include(f => f.Answers)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (form == null)
                return NotFound();

            if (form.UserId != GetCurrentUserId())
                return Forbid();

            return new FormDto
            {
                Id = form.Id,
                TemplateId = form.TemplateId,
                UserId = form.UserId,
                CreatedAt = form.CreatedAt,
                Answers = form.Answers.Select(a => new AnswerDto
                {
                    Id = a.Id,
                    QuestionId = a.QuestionId,
                    Value = a.Value
                }).ToList()
            };
        }

        // POST: api/forms
        [HttpPost]
        public async Task<ActionResult<FormDto>> CreateForm([FromBody] CreateFormRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var template = await _context.Templates
                .Include(t => t.Questions)
                .FirstOrDefaultAsync(t => t.Id == request.TemplateId);

            if (template == null)
                return NotFound("Template not found");

            var form = new Form
            {
                TemplateId = request.TemplateId,
                UserId = GetCurrentUserId(),
                CreatedAt = DateTime.UtcNow,
                Answers = request.Answers.Select(a => new Answer
                {
                    QuestionId = a.QuestionId,
                    Value = a.Value
                }).ToList()
            };

            _context.Forms.Add(form);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetForm), new { id = form.Id }, new FormDto
            {
                Id = form.Id,
                TemplateId = form.TemplateId,
                UserId = form.UserId,
                CreatedAt = form.CreatedAt,
                Answers = form.Answers.Select(a => new AnswerDto
                {
                    Id = a.Id,
                    QuestionId = a.QuestionId,
                    Value = a.Value
                }).ToList()
            });
        }

        // PUT: api/forms/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateForm(int id, Form updatedForm)
        {
            var form = await _context.Forms
                .Include(f => f.Answers)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (form == null)
                return NotFound();

            if (form.UserId != GetCurrentUserId() && !User.IsInRole("Admin"))
                return Forbid();

            form.Answers = updatedForm.Answers;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/forms/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteForm(int id)
        {
            var form = await _context.Forms
                .Include(f => f.Answers)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (form == null)
                return NotFound();

            if (form.UserId != GetCurrentUserId() && !User.IsInRole("Admin"))
                return Forbid();

            _context.Answers.RemoveRange(form.Answers);
            _context.Forms.Remove(form);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim ?? throw new UnauthorizedAccessException());
        }
    }

    public class CreateFormRequest
    {
        [Required(ErrorMessage = "TemplateId is required")]
        public int TemplateId { get; set; }

        [Required(ErrorMessage = "Answers are required")]
        [MinLength(1, ErrorMessage = "At least one answer is required")]
        public List<CreateAnswerRequest> Answers { get; set; }
    }

    public class CreateAnswerRequest
    {
        [Required(ErrorMessage = "QuestionId is required")]
        public int QuestionId { get; set; }

        [Required(ErrorMessage = "Value is required")]
        [StringLength(500, ErrorMessage = "Answer value cannot exceed 500 characters")]
        public string? Value { get; set; }
    }
}