using System.Text.RegularExpressions;
using Contracts;
using Typesense;

namespace Searchservice.MessageHandlers;

public class QuestionUpdatedHandler(ITypesenseClient client)
{
    public async Task HandleAsync(QuestionUpdated message)
    {
        await client.UpdateDocument("questions", message.QuestionId, new
        {
            message.Title,
            Content = StripeHtml(message.Content),
            Tags = message.Tags.ToArray()
        });
    }
    
    private static string StripeHtml(string content) 
    {
        return Regex.Replace(content, "<.*?>", string.Empty);
    }
}