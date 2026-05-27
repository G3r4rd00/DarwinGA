using DarwinGA.AI;
using DarwinGA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace DarwinGA.Evolutionals.BinaryEvolutional.Crossers
{
    /// <summary>
    /// AI-based population crossover operator.
    /// Serializes the entire population, sends it to an AI service (e.g., ChatGPT),
    /// and receives a crossed population back.
    /// Maintains conversation history to provide evolutionary context across generations.
    /// </summary>
    public class AICrosser : IPopulationCrosser<BinaryEvolutional>
    {
        private readonly IAIProvider _aiProvider;
        private readonly Func<List<BinaryEvolutional>,string> SerializePopulation;
        private readonly Func<string, List<BinaryEvolutional>> DeserializePopulation;

        /// <summary>
        /// Creates a new AI-based crossover operator.
        /// </summary>
        /// <param name="aiProvider">The AI provider to use for crossover operations.</param>
        /// <param name="populationSize">Configured population size for the crossover process.</param>
        /// <param name="serializePopulation">Function used to serialize parents before sending them to the AI.</param>
        /// <param name="deserializePopulation">Function used to deserialize AI response into offspring.</param>
        public AICrosser(
            IAIProvider aiProvider,
            int populationSize,
            Func<List<BinaryEvolutional>, string> serializePopulation,
            Func<string, List<BinaryEvolutional>> deserializePopulation,
            string systemMessage)
        {
            _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
            SerializePopulation = serializePopulation ?? throw new ArgumentNullException(nameof(serializePopulation));
            DeserializePopulation = deserializePopulation ?? throw new ArgumentNullException(nameof(deserializePopulation));
            systemMessage = systemMessage ?? throw new ArgumentNullException(nameof(systemMessage));
            _aiProvider.ClearChatHistory();
            _aiProvider.SetSystemMessage(systemMessage);
        }

        public List<BinaryEvolutional> CrossPopulation(List<BinaryEvolutional> parents)
        {
            if (parents == null || parents.Count == 0)
                return new List<BinaryEvolutional>();
            
            // 1. Serializar la población a JSON
            string serializedParents = SerializePopulation(parents);

            // 2. Enviar a la IA y recibir respuesta
            string aiResponse;
            try
            {
                // La llamada es asíncrona, pero la interfaz es síncrona
                // Usamos .Result para mantener compatibilidad
                aiResponse = _aiProvider.SendPromptAsync(serializedParents).Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling AI service: {ex.Message}");
                // En caso de error, devolvemos la población original
                return parents.ToList();
            }

            //3. Deserializar la respuesta
            List<BinaryEvolutional> offspring = DeserializePopulation(aiResponse);

            return offspring;
        }    
    }
}
