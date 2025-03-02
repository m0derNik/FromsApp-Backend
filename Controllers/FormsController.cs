using FormsApp.Api.Data;
using FormsApp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        public async Task<ActionResult<IEnumerable<FormDto>>> GetForms()
        {
            var userId = GetCurrentUserId();
            return await _context.Forms
                .Where(f => f.UserId == userId)
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
        public async Task<ActionResult<FormDto>> CreateForm(CreateFormRequest request)
        {
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

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim ?? throw new UnauthorizedAccessException());
        }
    }

    public class CreateFormRequest
    {
        public int TemplateId { get; set; }
        public List<CreateAnswerRequest> Answers { get; set; }
    }

    public class CreateAnswerRequest
    {
        public int QuestionId { get; set; }
        public string? Value { get; set; }
    }
}