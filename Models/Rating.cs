namespace FormsApp.Api.Models
{
    public class Rating
    {
        public int Id { get; set; }
        public int TemplateId { get; set; }
        public Template? Template { get; set; } // Связь с шаблоном
        public int UserId { get; set; }
        public User? User { get; set; }        // Кто поставил рейтинг
        public int Value { get; set; }         // Значение рейтинга (например, 1-5)
    }
}