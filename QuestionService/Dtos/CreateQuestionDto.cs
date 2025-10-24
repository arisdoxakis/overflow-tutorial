namespace QuestionService.Dtos;

public record CreateQuestionDto(string Title, string Content, List<string> Tags);