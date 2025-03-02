namespace FormsApp.Api.Models
{
    public class Answer
    {
        public int Id { get; set; }
        public int FormId { get; set; }         // Связь с формой
        public Form? Form { get; set; }         // Навигационное свойство
        public int QuestionId { get; set; }     // Связь с вопросом
        public Question? Question { get; set; } // Навигационное свойство
        public string? Value { get; set; }      // Ответ пользователя
    }
}