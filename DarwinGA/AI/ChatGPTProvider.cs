

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
        private readonly int _maxAccumulatedMessages;
        

        /// <summary>
        /// Creates a new ChatGPT provider.
        /// </summary>
        /// <param name="apiKey">Your OpenAI API key.</param>
        /// <param name="model">The model to use (default: gpt-3.5-turbo).</param>
        /// <param name="systemMessage">Optional system message to set the initial context for the conversation.</param>
        /// <param name="maxAccumulatedMessages">Maximum number of accumulated non-system messages kept in history (default: 3).</param>
        public ChatGPTProvider(string apiKey, string model = "gpt-3.5-turbo", int maxAccumulatedMessages = 3)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API key cannot be null or empty.", nameof(apiKey));

            if (maxAccumulatedMessages < 1)
                throw new ArgumentOutOfRangeException(nameof(maxAccumulatedMessages), "Max accumulated messages must be greater than 0.");

            _chatClient = new ChatClient(model, apiKey);
            _maxAccumulatedMessages = maxAccumulatedMessages;
            _messageHistory = new List<ChatMessage>();
        }

        public override void SetSystemMessage(string systemMessage)
        {
            ClearChatHistory();
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
            _messageHistory.Add(new UserChatMessage(prompt));
            TrimMessageHistory();

            // Send and get response
            var response = await _chatClient.CompleteChatAsync(_messageHistory);
            var text = response.Value.Content[0].Text;
            var parsedText = ParseResponse(text);

            _messageHistory.Add(new AssistantChatMessage(parsedText));
            TrimMessageHistory();

            // Return the response text
            return parsedText;
        }

        private void TrimMessageHistory()
        {
            int maxTotalMessages = 1 + _maxAccumulatedMessages;
            while (_messageHistory.Count > maxTotalMessages)
                _messageHistory.RemoveAt(1);
        }

        public override void ClearChatHistory()
        {
            _messageHistory.Clear();
        }
    }
}
