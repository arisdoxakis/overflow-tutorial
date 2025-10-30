using System.Text.RegularExpressions;
using Contracts;
using Searchservice.Models;
using Typesense;

namespace Searchservice.MessageHandlers;

public class QuestionCreatedHandler(ITypesenseClient client)
{
    public async Task HandleAsync(QuestionCreated message)
    {
        var created = new DateTimeOffset(message.Created).ToUnixTimeSeconds();

        var doc = new SearchQuestion()
        {
            Id = message.QuestionId,
            Title = message.Title,
            Content = StripeHtml(message.Content),
            CreatedAt = created,
            Tags = message.Tags.ToArray()
        };

        await client.CreateDocument("questions", doc);
        
        Console.WriteLine($"Document {message.QuestionId} has been created");
    }
    
    private static string StripeHtml(string content) 
    {
        return Regex.Replace(content, "<.*?>", string.Empty);
    }
}