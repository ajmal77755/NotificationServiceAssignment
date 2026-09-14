using Microsoft.Extensions.AI; 

namespace Notifications.Api.Integration.Tests
{
    public sealed class FakeChatClient : IChatClient
    {
        public string ResponseText { get; set; } =
            """
            {"title": "Fake title","category":"Performance","summary":"fake summary.","suggestedAction":"Fake action."
            """;

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken token = default)
        {
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, ResponseText)));
        }
        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken token = default) =>
            throw new NotSupportedException();

        public object? GetService(Type type, object? serviceKey = null) => null;

        public void Dispose() { }

    }
}
