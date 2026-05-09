using System.Threading.Tasks;

namespace DarwinGA.AI
{
    /// <summary>
    /// Interface for AI providers that can process genetic algorithm populations.
    /// </summary>
    public interface IAIProvider
    {
        /// <summary>
        /// Sends a prompt to the AI service and returns the response.
        /// </summary>
        /// <param name="prompt">The prompt to send to the AI.</param>
        /// <returns>The AI's response as a string.</returns>
        Task<string> SendPromptAsync(string prompt);
    }
}
