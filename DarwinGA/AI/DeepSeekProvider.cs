using DeepSeek;
using DeepSeek.Classes;
using OpenAI.Chat;

namespace DarwinGA.AI
{
    /// <summary>
    /// DeepSeek provider for AI-based genetic operations.
    /// Uses the DeepSeek.NET client to call DeepSeek models.
    /// </summary>
    public class DeepSeekProvider : IAIProvider
    {
        private readonly DeepSeekClient _deepSeekClient;
        private readonly string _model;
        private readonly List<Message> _messageHistory;
        private readonly int _maxAccumulatedMessages;

        /// <summary>
        /// Creates a new DeepSeek provider.
        /// </summary>
        /// <param name="apiKey">Your DeepSeek API key.</param>
        /// <param name="model">The model to use (default: deepseek-chat).</param>
        /// <param name="systemMessage">Optional system message to set the initial context for the conversation.</param>
        /// <param name="baseUrl">DeepSeek API base URL.</param>
        /// <param name="maxAccumulatedMessages">Maximum number of accumulated non-system messages kept in history (default: 3).</param>
        public DeepSeekProvider(string apiKey, string model = "deepseek-chat", string systemMessage = "", string baseUrl = "https://api.deepseek.com", int maxAccumulatedMessages = 3)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API key cannot be null or empty.", nameof(apiKey));

            if (string.IsNullOrWhiteSpace(model))
                throw new ArgumentException("Model cannot be null or empty.", nameof(model));

            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentException("Base URL cannot be null or empty.", nameof(baseUrl));

            if (maxAccumulatedMessages < 1)
                throw new ArgumentOutOfRangeException(nameof(maxAccumulatedMessages), "Max accumulated messages must be greater than 0.");

            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl.TrimEnd('/'))
            };

            _deepSeekClient = new DeepSeekClient(httpClient, apiKey);
            _model = model;

        

            _maxAccumulatedMessages = maxAccumulatedMessages;

            _messageHistory = new List<Message>();

        }

        public void ClearChatHistory()
        {
            _messageHistory.Clear();
        }

        public void SetSystemMessage(string systemMessage)
        {
            ClearChatHistory();
            if (string.IsNullOrWhiteSpace(systemMessage))
                throw new ArgumentException("System message cannot be null or empty.", nameof(systemMessage));

            Message m = new Message { Role = "system", Content = systemMessage };
            if (!_messageHistory.Any())
                _messageHistory.Add(m);
            else
                _messageHistory[0] = m;
        }

        public void ClearMessageHistory()
        {
            _messageHistory.Clear();
        }

        public async Task<string> SendPromptAsync(string prompt)
        {
            TrimMessageHistory();
            _messageHistory.Add(Message.NewUserMessage(prompt));
            
            var request = new ChatRequest
            {
                Model = _model,
                Messages = _messageHistory.ToArray()
            };

            var response = await _deepSeekClient.ChatAsync(request);
            var message = response?.Choices?.FirstOrDefault()?.Message?.Content;

            if (string.IsNullOrWhiteSpace(message))
                throw new InvalidOperationException(_deepSeekClient.ErrorMessage ?? "DeepSeek response did not include a message.");

            _messageHistory.Add(Message.NewAssistantMessage(message));
            TrimMessageHistory();

            return message;
        }

        private void TrimMessageHistory()
        {
            while (_messageHistory.Count > _maxAccumulatedMessages)
                _messageHistory.RemoveAt(1);
        }
    }
}
