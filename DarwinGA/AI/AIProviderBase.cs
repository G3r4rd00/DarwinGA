using System.Text.RegularExpressions;

namespace DarwinGA.AI
{
    /// <summary>
    /// Base class for AI providers.
    /// </summary>
    public abstract class AIProviderBase : IAIProvider
    {
        private static readonly Regex JsonCodeBlockRegex = new(
            "```json\\s*(?<json>[\\s\\S]*?)\\s*```",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex AnyCodeBlockRegex = new(
            "```\\s*(?<content>[\\s\\S]*?)\\s*```",
            RegexOptions.Compiled);

        public abstract Task<string> SendPromptAsync(string prompt);

        public abstract void SetSystemMessage(string systemMessage);

        /// <summary>
        /// Parses the AI response and extracts JSON when it is wrapped in markdown tags.
        /// </summary>
        /// <param name="response">Raw AI response.</param>
        /// <returns>Extracted JSON if found; otherwise the original response.</returns>
        protected string ParseResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return response;

            var jsonMatch = JsonCodeBlockRegex.Match(response);
            if (jsonMatch.Success)
                return jsonMatch.Groups["json"].Value.Trim();

            var anyMatch = AnyCodeBlockRegex.Match(response);
            if (anyMatch.Success)
                return anyMatch.Groups["content"].Value.Trim();

            return response;
        }
    }
}