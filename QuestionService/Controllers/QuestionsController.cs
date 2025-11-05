using System.Security.Claims;
using Contracts;
using FastExpressionCompiler;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestionService.Data;
using QuestionService.Dtos;
using QuestionService.Models;
using QuestionService.Services;
using Wolverine;

namespace QuestionService.Controllers;

[ApiController]
[Route("[controller]")]
public class QuestionsController(QuestionDbContext context, IMessageBus bus, TagService tagsService) : ControllerBase
{
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<Question>> CreateQuestion(CreateQuestionDto model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = User.FindFirstValue("name");

        if (userId is null || name is null)
            return BadRequest("Cannot get user details");

        if (!await tagsService.AreTagsValidAsync(model.Tags)) return BadRequest("Invalid tags");
        
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

        await bus.PublishAsync(new QuestionCreated(question.Id, question.Title, question.Content, question.CreatedAt, question.TagSlugs));

        return Created($"/questions/{question.Id}", question);
    }

    [HttpGet]
    public async Task<ActionResult<List<Question>>> GetQuestions(string? tag)
    {
        var query = context.Questions.AsQueryable();
        if (!string.IsNullOrEmpty(tag))
        {
            query = query.Where((x => x.TagSlugs.Contains(tag)));
        }

        return await query
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Question>> GetQuestion(string id)
    {
        var question = await context.Questions.FindAsync(id);

        if (question is null) return NotFound();

        await context.Questions.Where(x => x.Id == id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ViewCount, x => x.ViewCount + 1));

        return question;
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult> UpdateQuestion(string id, CreateQuestionDto dto)
    {
        var question = await context.Questions.FindAsync(id);
        if (question is null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != question.AskerId) return Unauthorized();

        if (!await tagsService.AreTagsValidAsync(dto.Tags)) return BadRequest("Invalid tags");

        question.Title = dto.Title;
        question.Content = dto.Content;
        question.TagSlugs = dto.Tags;
        question.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        await bus.PublishAsync(new QuestionUpdated(question.Id, question.Title, question.Content, question.TagSlugs.AsArray()));

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ActionResult> DeleteQuestion(string id)
    {
        var question = await context.Questions.FindAsync(id);
        if (question is null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != question.AskerId) return Unauthorized();
        
        context.Questions.Remove(question);
        await context.SaveChangesAsync();
        
        await bus.PublishAsync(new QuestionDeleted(question.Id));
        
        return NoContent();
    }
    
    [Authorize]
    [HttpPost("{questionId}/answers")]
    public async Task<ActionResult<Answer>> CreateAnswerForQuestion(string questionId, CreateAnswerDto model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = User.FindFirstValue("name");

        if (userId is null || name is null)
            return BadRequest("Cannot get user details");

        var answer = new Answer
        {
            QuestionId = questionId,
            Content = model.Content,
            UserId = userId,
            UserDisplayName = name
        };
        
        await context.Answers.AddAsync(answer);
        await context.SaveChangesAsync();

        return Created($"/questions/{answer.Id}", answer);
    }
    
    [Authorize]
    [HttpPut("/{questionId}/answers/{answerId}")]
    public async Task<ActionResult> UpdateAnswer(string questionId, string answerId, CreateAnswerDto model)
    {
        var question = await context.Questions.FindAsync(questionId);
        if (question is null) return NotFound();
        
        var answer = await context.Answers.FindAsync(answerId);
        if (answer is null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != question.AskerId) return Unauthorized();

        answer.Content = model.Content;
        question.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

       return NoContent();
    }
    
    [Authorize]
    [HttpDelete("/{questionId}/answers/{answerId}")]
    public async Task<ActionResult> DeleteQuestion(string questionId, string answerId)
    {
        var question = await context.Questions.FindAsync(questionId);
        if (question is null) return NotFound();
        
        var answer = await context.Answers.FindAsync(answerId);
        if (answer is null) return NotFound();
        
        if (answer.Accepted) return BadRequest("Cannot delete accepted answer");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != question.AskerId) return Unauthorized();
        
        context.Answers.Remove(answer);
        await context.SaveChangesAsync();
        
        return NoContent();
    }
    
    [Authorize]
    [HttpPost("/{questionId}/answers/{answerId}/accept")]
    public async Task<ActionResult> AcceptAnswer(string questionId, string answerId)
    {
        var question = await context.Questions.FindAsync(questionId);
        if (question is null) return NotFound();
        
        var answer = await context.Answers.FindAsync(answerId);
        if (answer is null) return NotFound();
        
        if (answer.Accepted) return BadRequest("Cannot delete accepted answer");

        answer.Accepted = !answer.Accepted;
        await context.SaveChangesAsync();
        
        return NoContent();
    }    
}