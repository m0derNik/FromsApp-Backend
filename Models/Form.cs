namespace FormsApp.Api.Models
{
    public class Form
    {
        public int Id { get; set; }
        public int TemplateId { get; set; }      // Связь с шаблоном
        public Template? Template { get; set; }  // Навигационное свойство
        public int UserId { get; set; }          // Кто заполнил форму
        public User? User { get; set; }          // Навигационное свойство
        public DateTime CreatedAt { get; set; }  // Когда форма была заполнена
        public List<Answer>? Answers { get; set; } // Ответы пользователя
    }
}