

using OpenAI.Chat;
using System.Text.Json;

namespace DarwinGA.AI
{
    /// <summary>
    /// ChatGPT/OpenAI provider for AI-based genetic operations.
    /// Maintains conversation history to provide context across multiple generations.
    /// </summary>
    public class ChatGPTProvider : IAIProvider
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
        public ChatGPTProvider(string apiKey, string model = "gpt-3.5-turbo", string systemMessage = "", int maxAccumulatedMessages = 3)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API key cannot be null or empty.", nameof(apiKey));

            if (maxAccumulatedMessages < 1)
                throw new ArgumentOutOfRangeException(nameof(maxAccumulatedMessages), "Max accumulated messages must be greater than 0.");

            _chatClient = new ChatClient(model, apiKey);

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

            _messageHistory = new List<ChatMessage>(){ 
                new SystemChatMessage(defaultSystemMessage)
            };
        }

        public async Task<string> SendPromptAsync(string prompt)
        {
            _messageHistory.Add(new UserChatMessage(prompt));
            TrimMessageHistory();

            // Send and get response
            var response = await _chatClient.CompleteChatAsync(_messageHistory);
            var text = response.Value.Content[0].Text;

            _messageHistory.Add(new AssistantChatMessage(text));
            TrimMessageHistory();

            // Return the response text
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
