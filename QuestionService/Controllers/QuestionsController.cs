using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        var validTags = await context.Tags
            .AsNoTracking()
            .Where(x => model.Tags.Contains(x.Slug))
            .ToListAsync();
        var missing = model.Tags.Except(validTags.Select(x => x.Slug).ToList()).ToList();
        if (missing.Count != 0) return BadRequest($"Invalid tags: {string.Join(",", missing)}");
        
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
        
        var validTags = await context.Tags
            .AsNoTracking()
            .Where(x => dto.Tags.Contains(x.Slug))
            .ToListAsync();
        var missing = dto.Tags.Except(validTags.Select(x => x.Slug).ToList()).ToList();
        if (missing.Count != 0) return BadRequest($"Invalid tags: {string.Join(",", missing)}");
        
        question.Title = dto.Title;
        question.Content = dto.Content;
        question.TagSlugs = dto.Tags;
        question.UpdatedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync();
        
        return NoContent();
    }

    [HttpDelete]
    [Authorize]
    public async Task<ActionResult> DeleteQuestion(string id)
    {
        var question = await context.Questions.FindAsync(id);
        if (question is null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != question.AskerId) return Unauthorized();
        
        context.Questions.Remove(question);
        await context.SaveChangesAsync();
        
        return NoContent();
    }
}