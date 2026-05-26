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
        
        /// <summary>
        /// Creates a new AI-based crossover operator.
        /// </summary>
        /// <param name="aiProvider">The AI provider to use for crossover operations.</param>
        public AICrosser(IAIProvider aiProvider, int populationSize)
        {
            _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
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

        

       

        private string SerializePopulation(List<BinaryEvolutional> population)
        {
            return JsonSerializer.Serialize(population.Select(r => r._genes.Select(g => g ? 1 : 0)));
        }

        private List<BinaryEvolutional> DeserializePopulation(string response)
        {
            List<int[]> boolArrays = JsonSerializer.Deserialize<List<int[]>>(response);
            List<BinaryEvolutional> offspring = new List<BinaryEvolutional>();
            foreach (var sIndividual in boolArrays)
            {
                var individual = new BinaryEvolutional(sIndividual.Select(b => b == 1).ToArray());
                offspring.Add(individual);
            }

            return offspring;
        }
    }
}
