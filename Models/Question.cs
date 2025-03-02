namespace FormsApp.Api.Models
{
    public class Question
    {
        public int Id { get; set; }
        public string? Text { get; set; }         // Текст вопроса
        public string? Type { get; set; }         // Тип вопроса (text, multipleChoice, etc.)
        public string? Options { get; set; }      // Опции для выбора (JSON-строка, если multipleChoice)
        public int TemplateId { get; set; }       // ID шаблона
        public Template? Template { get; set; }   // Связь с шаблоном
    }
}