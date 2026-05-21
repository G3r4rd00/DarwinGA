using OpenAI.Chat;
using System.ClientModel;

namespace DarwinGA.AI
{
    /// <summary>
    /// LM Studio provider using the OpenAI-compatible local REST API.
    /// </summary>
    public class LMStudioProvider : IAIProvider
    {
        private readonly ChatClient _chatClient;
        private readonly List<ChatMessage> _messageHistory;
        private readonly int _maxAccumulatedMessages;

        /// <summary>
        /// Creates a new LM Studio provider.
        /// </summary>
        /// <param name="baseUrl">LM Studio OpenAI-compatible API base URL (for example: http://localhost:1234/v1).</param>
        /// <param name="model">Model identifier loaded in LM Studio.</param>
        /// <param name="apiKey">Optional API key. LM Studio usually accepts any non-empty token.</param>
        /// <param name="systemMessage">Optional system message.</param>
        /// <param name="maxAccumulatedMessages">Maximum number of accumulated non-system messages kept in history (default: 3).</param>
        public LMStudioProvider(string baseUrl, string model, string? apiKey = null, string systemMessage = "", int maxAccumulatedMessages = 3)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentException("Base URL cannot be null or empty.", nameof(baseUrl));

            if (string.IsNullOrWhiteSpace(model))
                throw new ArgumentException("Model cannot be null or empty.", nameof(model));

            if (maxAccumulatedMessages < 1)
                throw new ArgumentOutOfRangeException(nameof(maxAccumulatedMessages), "Max accumulated messages must be greater than 0.");

            var options = new OpenAI.OpenAIClientOptions
            {
                Endpoint = new Uri(baseUrl.TrimEnd('/'))
            };

            var token = string.IsNullOrWhiteSpace(apiKey) ? "lm-studio" : apiKey;
            _chatClient = new ChatClient(model, new ApiKeyCredential(token), options);

            string defaultSystemMessage = @"You are a genetic algorithm assistant specialized in evolutionary computation. 
You receive parents of binary chromosomes in JSON format and perform crossover operations to create offspring.
Your goal is to apply intelligent crossover strategies that:
1. Preserve good genetic material from fit parents
2. Explore new combinations that might improve fitness
3. Maintain population diversity
4. Learn from previous generations to improve crossover quality

Always respond with ONLY a valid JSON array of binary strings. Do not include explanations or additional text.
Learn from the evolutionary progress across generations to make better crossover decisions.";

            if (!string.IsNullOrWhiteSpace(systemMessage))
                defaultSystemMessage = systemMessage;

            _maxAccumulatedMessages = maxAccumulatedMessages;

            _messageHistory = new List<ChatMessage>()
            {
                new SystemChatMessage(defaultSystemMessage)
            };
        }

        public async Task<string> SendPromptAsync(string prompt)
        {
            _messageHistory.Add(new UserChatMessage(prompt));
            TrimMessageHistory();

            var response = await _chatClient.CompleteChatAsync(_messageHistory);
            var text = response.Value.Content[0].Text;

            _messageHistory.Add(new AssistantChatMessage(text));
            TrimMessageHistory();

            return text;
        }

        private void TrimMessageHistory()
        {
            int maxTotalMessages = 1 + _maxAccumulatedMessages;
            while (_messageHistory.Count > maxTotalMessages)
            {
                _messageHistory.RemoveAt(1);
            }
        }
    }
}
