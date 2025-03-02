namespace FormsApp.Api.Models
{
    public class FormDto
    {
        public int Id { get; set; }
        public int TemplateId { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<AnswerDto>? Answers { get; set; }
    }

    public class AnswerDto
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public string? Value { get; set; }
    }
}