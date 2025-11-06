using Contracts;
using Typesense;

namespace Searchservice.MessageHandlers;

public class AnswerCountUpdatedHandler(ITypesenseClient client)
{
    public async Task HandleAsync(UpdatedAnswerCount message)
    {
        await client.UpdateDocument("questions", message.QuestionId, new
        {
            message.AnswerCount
        });
    }
}