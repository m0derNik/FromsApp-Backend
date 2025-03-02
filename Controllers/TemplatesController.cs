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
    public class TemplatesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TemplatesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<TemplateDto>>> GetTemplates()
        {
            return await _context.Templates
                .Where(t => t.IsPublic)
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
                    }).ToList()
                })
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TemplateDto>> GetTemplate(int id)
        {
            var template = await _context.Templates
                .Include(t => t.Questions)
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
                }).ToList()
            };
        }

        [HttpPost]
        public async Task<ActionResult<Template>> CreateTemplate(Template template)
        {
            template.UserId = GetCurrentUserId();
            _context.Templates.Add(template);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTemplate), new { id = template.Id }, template);
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim ?? throw new UnauthorizedAccessException());
        }
    }
}