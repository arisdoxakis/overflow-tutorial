using Contracts;
using Typesense;

namespace Searchservice.MessageHandlers;

public class QuestionDeletedHandler(ITypesenseClient client)
{
    public async Task HandleAsync(QuestionDeleted message)
    {
        await client.DeleteDocuments("questions", message.QuestionId);
    }
}