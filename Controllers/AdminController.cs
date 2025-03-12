using FormsApp.Api.Data;
using FormsApp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FormsApp.Api.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")] // Доступ только для админов
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // DELETE: api/admin/users/{id}
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            // Удаляем связанные данные
            var ratings = await _context.Ratings.Where(r => r.UserId == id).ToListAsync();
            _context.Ratings.RemoveRange(ratings);

            var forms = await _context.Forms.Where(f => f.UserId == id).ToListAsync();
            _context.Forms.RemoveRange(forms);

            var templates = await _context.Templates
                .Include(t => t.Questions)
                .Include(t => t.Forms).ThenInclude(f => f.Answers)
                .Where(t => t.UserId == id)
                .ToListAsync();
            foreach (var template in templates)
            {
                _context.Answers.RemoveRange(template.Forms.SelectMany(f => f.Answers));
                _context.Forms.RemoveRange(template.Forms);
                _context.Questions.RemoveRange(template.Questions);
                _context.Templates.Remove(template);
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/admin/templates/{id}
        [HttpDelete("templates/{id}")]
        public async Task<IActionResult> DeleteTemplate(int id)
        {
            var template = await _context.Templates
                .Include(t => t.Questions)
                .Include(t => t.Forms).ThenInclude(f => f.Answers)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (template == null)
                return NotFound();

            _context.Answers.RemoveRange(template.Forms.SelectMany(f => f.Answers));
            _context.Forms.RemoveRange(template.Forms);
            _context.Questions.RemoveRange(template.Questions);
            _context.Templates.Remove(template);

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PUT: api/admin/templates/{id}
        [HttpPut("templates/{id}")]
        public async Task<IActionResult> UpdateTemplate(int id, Template updatedTemplate)
        {
            var template = await _context.Templates
                .Include(t => t.Questions)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (template == null)
                return NotFound();

            template.Title = updatedTemplate.Title;
            template.Description = updatedTemplate.Description;
            template.IsPublic = updatedTemplate.IsPublic;
            template.Questions = updatedTemplate.Questions;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PUT: api/admin/forms/{id}
        [HttpPut("forms/{id}")]
        public async Task<IActionResult> UpdateForm(int id, Form updatedForm)
        {
            var form = await _context.Forms
                .Include(f => f.Answers)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (form == null)
                return NotFound();

            form.Answers = updatedForm.Answers;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}