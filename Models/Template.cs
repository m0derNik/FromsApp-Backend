namespace FormsApp.Api.Models
{
    public class Template
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public bool IsPublic { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public List<Question>? Questions { get; set; }
        public List<Form>? Forms { get; set; }
        public List<Rating>? Ratings { get; set; } // Добавляем связь с рейтингами
    }
}