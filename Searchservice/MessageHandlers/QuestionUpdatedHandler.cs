using System.Text.RegularExpressions;
using Contracts;
using Searchservice.Models;
using Typesense;

namespace Searchservice.MessageHandlers;

public class QuestionUpdatedHandler(ITypesenseClient client)
{
    public async Task HandleAsync(QuestionUpdated message)
    {
        var doc = new SearchQuestion
        {
            Id = message.QuestionId,
            Title = message.Title,
            Content = StripeHtml(message.Content),
            Tags = message.Tags.ToArray()
        };

        await client.UpdateDocument("questions", doc.Id, doc);
    }
    
    private static string StripeHtml(string content) 
    {
        return Regex.Replace(content, "<.*?>", string.Empty);
    }
}