using OpenAI.Chat;
using System.ClientModel;

namespace DarwinGA.AI
{
    /// <summary>
    /// LM Studio provider using the OpenAI-compatible local REST API.
    /// </summary>
    public class LMStudioProvider : AIProviderBase
    {
        private readonly ChatClient _chatClient;
        private string _systemMessage = string.Empty;

        /// <summary>
        /// Creates a new LM Studio provider.
        /// </summary>
        /// <param name="baseUrl">LM Studio OpenAI-compatible API base URL (for example: http://localhost:1234/v1).</param>
        /// <param name="model">Model identifier loaded in LM Studio.</param>
        /// <param name="apiKey">Optional API key. LM Studio usually accepts any non-empty token.</param>
        /// <param name="systemMessage">Optional system message.</param>
        public LMStudioProvider(string baseUrl, string model, string? apiKey = null, string systemMessage = "")
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentException("Base URL cannot be null or empty.", nameof(baseUrl));

            if (string.IsNullOrWhiteSpace(model))
                throw new ArgumentException("Model cannot be null or empty.", nameof(model));

            var options = new OpenAI.OpenAIClientOptions
            {
                Endpoint = new Uri(baseUrl.TrimEnd('/'))
            };

            var token = string.IsNullOrWhiteSpace(apiKey) ? "lm-studio" : apiKey;
            _chatClient = new ChatClient(model, new ApiKeyCredential(token), options);

            if (!string.IsNullOrWhiteSpace(systemMessage))
                _systemMessage = systemMessage;
        }


        public override async Task<string> SendPromptAsync(string prompt)
        {
            var messages = new List<ChatMessage>();
            if (!string.IsNullOrWhiteSpace(_systemMessage))
                messages.Add(new SystemChatMessage(_systemMessage));
            messages.Add(new UserChatMessage(prompt));

            var response = await _chatClient.CompleteChatAsync(messages);
            var text = response.Value.Content[0].Text;
            var parsedText = ParseResponse(text);

            return parsedText;
        }

        public override void SetSystemMessage(string systemMessage)
        {
            if (string.IsNullOrWhiteSpace(systemMessage))
                throw new ArgumentException("System message cannot be null or empty.", nameof(systemMessage));
            _systemMessage = systemMessage;
        }
    }
}
