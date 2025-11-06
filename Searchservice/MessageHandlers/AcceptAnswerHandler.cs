using Contracts;
using Typesense;

namespace Searchservice.MessageHandlers;

public class AcceptAnswerHandler(ITypesenseClient client)
{
    public async Task HandleAsync(AnswerAccepted message)
    {
        await client.UpdateDocument("questions", message.QuestionId, new { HasAcceptedAnswer = true });
    }
}