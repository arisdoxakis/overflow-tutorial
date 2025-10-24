using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestionService.Data;
using QuestionService.Dtos;
using QuestionService.Models;

namespace QuestionService.Controllers;

[ApiController]
[Route("[controller]")]
public class QuestionsController(QuestionDbContext context) : ControllerBase
{
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<Question>> CreateQuestion(CreateQuestionDto model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = User.FindFirstValue("name");

        if (userId is null || name is null)
            return BadRequest("Cannot get user details");

        var question = new Question
        {
            Title = model.Title,
            Content = model.Content,
            TagSlugs = model.Tags,
            AskerId = userId,
            AskerDisplayName = name
        };

        await context.Questions.AddAsync(question);
        await context.SaveChangesAsync();

        return Created($"/questions/{question.Id}", question);
    }
}