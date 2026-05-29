using DeepSeek;
using DeepSeek.Classes;
using OpenAI.Chat;

namespace DarwinGA.AI
{
    /// <summary>
    /// DeepSeek provider for AI-based genetic operations.
    /// Uses the DeepSeek.NET client to call DeepSeek models.
    /// </summary>
    public class DeepSeekProvider : AIProviderBase
    {
        private readonly DeepSeekClient _deepSeekClient;
        private readonly string _model;
        private readonly List<Message> _messageHistory;

        /// <summary>
        /// Creates a new DeepSeek provider.
        /// </summary>
        /// <param name="apiKey">Your DeepSeek API key.</param>
        /// <param name="model">The model to use (default: deepseek-chat).</param>
        /// <param name="systemMessage">Optional system message to set the initial context for the conversation.</param>
        /// <param name="baseUrl">DeepSeek API base URL.</param>
        public DeepSeekProvider(string apiKey, string model = "deepseek-chat", string systemMessage = "", string baseUrl = "https://api.deepseek.com")
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API key cannot be null or empty.", nameof(apiKey));

            if (string.IsNullOrWhiteSpace(model))
                throw new ArgumentException("Model cannot be null or empty.", nameof(model));

            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentException("Base URL cannot be null or empty.", nameof(baseUrl));

            

            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl.TrimEnd('/'))
            };

            _deepSeekClient = new DeepSeekClient(httpClient, apiKey);
            _model = model;

        

            _messageHistory = new List<Message>();

        }


        public override void SetSystemMessage(string systemMessage)
        {
            if (string.IsNullOrWhiteSpace(systemMessage))
                throw new ArgumentException("System message cannot be null or empty.", nameof(systemMessage));

            Message m = new Message { Role = "system", Content = systemMessage };
            if (!_messageHistory.Any())
                _messageHistory.Add(m);
            else
                _messageHistory[0] = m;
        }

        
        public override async Task<string> SendPromptAsync(string prompt)
        {
            _messageHistory.RemoveAll(m => m.Role != "system"); // Keep only system messages
            _messageHistory.Add(Message.NewUserMessage(prompt));
            
            var request = new ChatRequest
            {
                Model = _model,
                Messages = _messageHistory.ToArray()
            };

            var response = await _deepSeekClient.ChatAsync(request);
            
            var message = response?.Choices?.FirstOrDefault()?.Message;
            if(message== null)
                throw new Exception("DeepSeek response did not include any choices or messages.");

            var responseContent = !string.IsNullOrWhiteSpace(message.Content)
                ? message.Content
                : message.ReasoningContent;

            if (string.IsNullOrWhiteSpace(responseContent))
                throw new InvalidOperationException(_deepSeekClient.ErrorMessage ?? "DeepSeek response did not include a message.");

            var parsedMessage = ParseResponse(responseContent);

            _messageHistory.Add(Message.NewAssistantMessage(parsedMessage));
            

            return parsedMessage;
        }

    }
}
