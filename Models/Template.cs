namespace FormsApp.Api.Models
{
    public class Template
    {
        public int Id { get; set; }
        public string? Title { get; set; }         // Название шаблона
        public string? Description { get; set; }   // Описание шаблона
        public bool IsPublic { get; set; }         // Публичный или приватный
        public int UserId { get; set; }            // ID создателя шаблона
        public User? User { get; set; }            // Связь с пользователем
        public List<Question>? Questions { get; set; } // Вопросы в шаблоне
    }
}