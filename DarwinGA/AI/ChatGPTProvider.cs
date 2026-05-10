using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DarwinGA.AI
{
    /// <summary>
    /// ChatGPT/OpenAI provider for AI-based genetic operations.
    /// Maintains conversation history to provide context across multiple generations.
    /// </summary>
    public class ChatGPTProvider : IAIProvider
    {
        // Lista de modelos válidos según https://developers.openai.com/api/docs/models/all (junio 2024)
        public static readonly HashSet<string> AllowedModels = new(StringComparer.OrdinalIgnoreCase)
        {
            "gpt-3.5-turbo",
            "gpt-3.5-turbo-0125",
            "gpt-3.5-turbo-1106",
            "gpt-3.5-turbo-0613",
            "gpt-3.5-turbo-16k",
            "gpt-4",
            "gpt-4-0613",
            "gpt-4-32k",
            "gpt-4-32k-0613",
            "gpt-4-turbo",
            "gpt-4-0125-preview",
            "gpt-4-1106-preview",
            "gpt-4o"
        };

        // Modelos que NO admiten temperature (según docs OpenAI)
        private static readonly HashSet<string> NoTemperatureModels = new(StringComparer.OrdinalIgnoreCase)
        {
            "gpt-4o",
            "gpt-4-turbo",
            "gpt-4-0125-preview",
            "gpt-4-1106-preview"
        };
        private readonly string _apiKey;
        private readonly string _model;
        private readonly HttpClient _httpClient;
        private readonly List<ChatMessage> _conversationHistory;
        private const string OpenAIEndpoint = "https://api.openai.com/v1/chat/completions";

        /// <summary>
        /// Creates a new ChatGPT provider.
        /// </summary>
        /// <param name="apiKey">Your OpenAI API key.</param>
        /// <param name="model">The model to use (default: gpt-3.5-turbo).</param>
        /// <param name="systemMessage">Optional system message to set the initial context for the conversation.</param>
        public ChatGPTProvider(string apiKey, string model = "gpt-3.5-turbo", string systemMessage = "")
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API key cannot be null or empty.", nameof(apiKey));


            if (!AllowedModels.Contains(model))
                throw new ArgumentException($"Modelo '{model}' no permitido. Usa uno de: {string.Join(", ", AllowedModels)}", nameof(model));

            _apiKey = apiKey;
            _model = model;
            _httpClient = new HttpClient();
            _conversationHistory = new List<ChatMessage>();
            string defaultSystemMessage = @"You are a genetic algorithm assistant specialized in evolutionary computation. 
You receive parents of binary chromosomes in JSON format and perform crossover operations to create offspring.
Your goal is to apply intelligent crossover strategies that:
1. Preserve good genetic material from fit parents
2. Explore new combinations that might improve fitness
3. Maintain population diversity
4. Learn from previous generations to improve crossover quality

Always respond with ONLY a valid JSON array of binary strings. Do not include explanations or additional text.
Learn from the evolutionary progress across generations to make better crossover decisions.";

            // Initialize with system message
            _conversationHistory.Add(new ChatMessage
            {
                Role = "system",
                Content = string.IsNullOrEmpty(systemMessage) ? defaultSystemMessage : systemMessage
            });
        }

        /// <summary>
        /// Gets the number of messages in the conversation history.
        /// </summary>
        public int ConversationLength => _conversationHistory.Count;

        /// <summary>
        /// Clears the conversation history (keeps only the system message).
        /// </summary>
        public void ResetConversation()
        {
            var systemMessage = _conversationHistory[0];
            _conversationHistory.Clear();
            _conversationHistory.Add(systemMessage);
        }

        public async Task<string> SendPromptAsync(string prompt)
        {
            // Add user message to history
            _conversationHistory.Add(new ChatMessage
            {
                Role = "user",
                Content = prompt
            });

            // Build request with full conversation history
            object requestBody;
            var messages = _conversationHistory.Select(m => new
            {
                role = m.Role,
                content = m.Content
            }).ToArray();
            if (NoTemperatureModels.Contains(_model))
            {
                requestBody = new
                {
                    model = _model,
                    messages
                };
            }
            else
            {
                requestBody = new
                {
                    model = _model,
                    messages,
                    temperature = 0.7
                };
            }

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var response = await _httpClient.PostAsync(OpenAIEndpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"OpenAI API error: {response.StatusCode} - {responseContent}");
            }

            var jsonResponse = JsonDocument.Parse(responseContent);
            var messageContent = jsonResponse.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            var assistantResponse = messageContent ?? string.Empty;

            // Add assistant response to history
            _conversationHistory.Add(new ChatMessage
            {
                Role = "assistant",
                Content = assistantResponse
            });

            return assistantResponse;
        }

        /// <summary>
        /// Represents a message in the conversation history.
        /// </summary>
        private class ChatMessage
        {
            public string Role { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
        }
    }
}
