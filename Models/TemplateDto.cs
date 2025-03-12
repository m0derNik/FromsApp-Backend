namespace FormsApp.Api.Models
{
    public class TemplateDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public bool IsPublic { get; set; }
        public int UserId { get; set; }
        public List<QuestionDto>? Questions { get; set; }
        public double AverageRating { get; set; } // Средний рейтинг
    }

    public class QuestionDto
    {
        public int Id { get; set; }
        public string? Text { get; set; }
        public string? Type { get; set; }
        public string? Options { get; set; }
    }
}