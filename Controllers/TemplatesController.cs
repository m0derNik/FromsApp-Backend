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
    public class TemplatesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TemplatesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<PagedResponse<TemplateDto>>> GetTemplates(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10)
        {
            page = Math.Max(1, page); // Убеждаемся, что page >= 1
            var query = _context.Templates.Where(t => t.IsPublic);
            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TemplateDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    IsPublic = t.IsPublic,
                    UserId = t.UserId,
                    Questions = t.Questions.Select(q => new QuestionDto
                    {
                        Id = q.Id,
                        Text = q.Text,
                        Type = q.Type,
                        Options = q.Options
                    }).ToList(),
                    AverageRating = t.Ratings.Any() ? t.Ratings.Average(r => r.Value) : 0
                })
                .ToListAsync();

            return new PagedResponse<TemplateDto>(items, totalCount, page, pageSize);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TemplateDto>> GetTemplate(int id)
        {
            var template = await _context.Templates
                .Include(t => t.Questions)
                .Include(t => t.Ratings) // Подгружаем рейтинги
                .FirstOrDefaultAsync(t => t.Id == id);

            if (template == null)
                return NotFound();

            if (!template.IsPublic && template.UserId != GetCurrentUserId())
                return Forbid();

            return new TemplateDto
            {
                Id = template.Id,
                Title = template.Title,
                Description = template.Description,
                IsPublic = template.IsPublic,
                UserId = template.UserId,
                Questions = template.Questions.Select(q => new QuestionDto
                {
                    Id = q.Id,
                    Text = q.Text,
                    Type = q.Type,
                    Options = q.Options
                }).ToList(),
                AverageRating = template.Ratings.Any() ? template.Ratings.Average(r => r.Value) : 0
            };
        }

        [HttpPost]
        public async Task<ActionResult<Template>> CreateTemplate([FromBody] CreateTemplateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var template = new Template
            {
                Title = request.Title,
                Description = request.Description,
                IsPublic = request.IsPublic,
                UserId = GetCurrentUserId(),
                Questions = request.Questions.Select(q => new Question
                {
                    Text = q.Text,
                    Type = q.Type,
                    Options = q.Options
                }).ToList()
            };

            _context.Templates.Add(template);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTemplate), new { id = template.Id }, template);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTemplate(int id, Template updatedTemplate)
        {
            var template = await _context.Templates
                .Include(t => t.Questions)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (template == null)
                return NotFound();

            if (template.UserId != GetCurrentUserId() && !User.IsInRole("Admin"))
                return Forbid();

            template.Title = updatedTemplate.Title;
            template.Description = updatedTemplate.Description;
            template.IsPublic = updatedTemplate.IsPublic;
            template.Questions = updatedTemplate.Questions;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTemplate(int id)
        {
            var template = await _context.Templates
                .Include(t => t.Questions)
                .Include(t => t.Forms).ThenInclude(f => f.Answers)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (template == null)
                return NotFound();

            if (template.UserId != GetCurrentUserId() && !User.IsInRole("Admin"))
                return Forbid();

            _context.Answers.RemoveRange(template.Forms.SelectMany(f => f.Answers));
            _context.Forms.RemoveRange(template.Forms);
            _context.Questions.RemoveRange(template.Questions);
            _context.Templates.Remove(template);

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("{id}/forms")]
        public async Task<ActionResult<IEnumerable<FormDto>>> GetTemplateForms(int id)
        {
            var template = await _context.Templates
                .Include(t => t.Forms).ThenInclude(f => f.Answers)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (template == null)
                return NotFound("Template not found");

            if (template.UserId != GetCurrentUserId() && !User.IsInRole("Admin"))
                return Forbid("You are not the owner of this template");

            return template.Forms.Select(f => new FormDto
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
            }).ToList();
        }

        // GET: api/templates/search?query={searchString}
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<ActionResult<PagedResponse<TemplateDto>>> SearchTemplates(
    [FromQuery] string query,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10)
        {
            page = Math.Max(1, page); // Убеждаемся, что page >= 1
            if (string.IsNullOrWhiteSpace(query))
                return await GetTemplates(page, pageSize);

            var lowerQuery = query.ToLower();
            var searchableQuery = _context.Templates
                .Where(t => t.IsPublic &&
                           (t.Title.ToLower().Contains(lowerQuery) ||
                            t.Description.ToLower().Contains(lowerQuery)));

            var totalCount = await searchableQuery.CountAsync();

            var items = await searchableQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TemplateDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    IsPublic = t.IsPublic,
                    UserId = t.UserId,
                    Questions = t.Questions.Select(q => new QuestionDto
                    {
                        Id = q.Id,
                        Text = q.Text,
                        Type = q.Type,
                        Options = q.Options
                    }).ToList(),
                    AverageRating = t.Ratings.Any() ? t.Ratings.Average(r => r.Value) : 0
                })
                .ToListAsync();

            return new PagedResponse<TemplateDto>(items, totalCount, page, pageSize);
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim ?? throw new UnauthorizedAccessException());
        }

        private bool IsAdmin()
        {
            return User.IsInRole("Admin");
        }
    }

    public class CreateTemplateRequest
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 100 characters")]
        public string? Title { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        public bool IsPublic { get; set; }

        [Required(ErrorMessage = "At least one question is required")]
        [MinLength(1, ErrorMessage = "At least one question is required")]
        public List<CreateQuestionRequest> Questions { get; set; }
    }

    public class CreateQuestionRequest
    {
        [Required(ErrorMessage = "Text is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Question text must be between 1 and 200 characters")]
        public string? Text { get; set; }

        [Required(ErrorMessage = "Type is required")]
        [RegularExpression("text|multipleChoice", ErrorMessage = "Type must be either 'text' or 'multipleChoice'")]
        public string? Type { get; set; }

        [StringLength(500, ErrorMessage = "Options cannot exceed 500 characters")]
        public string? Options { get; set; }
    }
}