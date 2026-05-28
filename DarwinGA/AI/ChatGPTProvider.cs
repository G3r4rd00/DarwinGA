

using OpenAI.Chat;
using System.Text.Json;

namespace DarwinGA.AI
{
    /// <summary>
    /// ChatGPT/OpenAI provider for AI-based genetic operations.
    /// Maintains conversation history to provide context across multiple generations.
    /// </summary>
    public class ChatGPTProvider : AIProviderBase
    {
        private readonly ChatClient _chatClient;
        private readonly List<ChatMessage> _messageHistory;

        /// <summary>
        /// Creates a new ChatGPT provider.
        /// </summary>
        /// <param name="apiKey">Your OpenAI API key.</param>
        /// <param name="model">The model to use (default: gpt-3.5-turbo).</param>
        /// <param name="systemMessage">Optional system message to set the initial context for the conversation.</param>
        /// <param name="maxAccumulatedMessages">Maximum number of accumulated non-system messages kept in history (default: 3).</param>
        public ChatGPTProvider(string apiKey, string model = "gpt-3.5-turbo")
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API key cannot be null or empty.", nameof(apiKey));

            

            _chatClient = new ChatClient(model, apiKey);
            _messageHistory = new List<ChatMessage>();
        }

        public override void SetSystemMessage(string systemMessage)
        {
            if (string.IsNullOrWhiteSpace(systemMessage))
                throw new ArgumentException("System message cannot be null or empty.", nameof(systemMessage));
            // Update the system message in the history
            if (_messageHistory.Count > 0 && _messageHistory[0] is SystemChatMessage)
                _messageHistory[0] = new SystemChatMessage(systemMessage);
            else
                _messageHistory.Insert(0, new SystemChatMessage(systemMessage));
        }

        public override async Task<string> SendPromptAsync(string prompt)
        {
            _messageHistory.RemoveAll(m => m is not SystemChatMessage); // Keep only system messages
            _messageHistory.Add(new UserChatMessage(prompt));
            
            // Send and get response
            var response = await _chatClient.CompleteChatAsync(_messageHistory);
            var text = response.Value.Content[0].Text;
            var parsedText = ParseResponse(text);

            _messageHistory.Add(new AssistantChatMessage(parsedText));
            
            // Return the response text
            return parsedText;
        }


    }
}
