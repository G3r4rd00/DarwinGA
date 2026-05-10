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
        private int _generationCount = 0;
        private double _lastBestFitness = 0;
        private double _lastAverageFitness = 0;
        private string _additionalContext = "";
        private int _populationSize = 0;

        /// <summary>
        /// Creates a new AI-based crossover operator.
        /// </summary>
        /// <param name="aiProvider">The AI provider to use for crossover operations.</param>
        /// <param name="additionalContext">Additional context to provide to the AI for crossover operations.</param>
        public AICrosser(IAIProvider aiProvider, int populationSize, string additionalContext = "")
        {
            _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
            _additionalContext = additionalContext; 
            _populationSize = populationSize;
        }

        public List<BinaryEvolutional> CrossPopulation(List<BinaryEvolutional> parents)
        {
            if (parents == null || parents.Count == 0)
                return new List<BinaryEvolutional>();

            _generationCount++;

            
            // 2. Serializar la población a JSON
            string serializedParents = SerializePopulation(parents);

            // 3. Crear el prompt para la IA con contexto evolutivo
            string prompt = BuildPrompt(serializedParents, _populationSize, _additionalContext);

            // 4. Enviar a la IA y recibir respuesta
            string aiResponse;
            try
            {
                // La llamada es asíncrona, pero la interfaz es síncrona
                // Usamos .Result para mantener compatibilidad
                aiResponse = _aiProvider.SendPromptAsync(prompt).Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling AI service: {ex.Message}");
                // En caso de error, devolvemos la población original
                return parents.ToList();
            }

            // 5. Extraer el JSON de la respuesta (puede venir con texto adicional)
            string jsonResponse = ExtractJson(aiResponse);

            // 6. Deserializar la respuesta
            List<BinaryEvolutional> offspring = DeserializePopulation(jsonResponse, parents[0].Size);

            return offspring;
        }

       

        private string GetChromosomeString(BinaryEvolutional individual)
        {
            var genes = new StringBuilder();
            for (int i = 0; i < individual.Size; i++)
            {
                genes.Append(individual.GetGen(i) ? "1" : "0");
            }
            return genes.ToString();
        }

        private string BuildPrompt(string populationJson, int expectedSize, string additionalContext = "")
        {
            var promptBuilder = new StringBuilder();

            promptBuilder.AppendLine($"Generation {_generationCount}:");
            promptBuilder.AppendLine($"Best fitness: {_lastBestFitness:F2} | Average fitness: {_lastAverageFitness:F2}");

            promptBuilder.AppendLine();
            promptBuilder.AppendLine($"Input population (with fitness scores):");
            promptBuilder.AppendLine(populationJson);

            // Only include additional context if provided (for backwards compatibility)
            if (!string.IsNullOrWhiteSpace(additionalContext))
            {
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("Additional context:");
                promptBuilder.AppendLine(additionalContext);
            }

            promptBuilder.AppendLine();
            promptBuilder.AppendLine($"Create exactly {expectedSize} offspring chromosomes.");
            promptBuilder.AppendLine("Return ONLY a JSON array of binary strings (0s and 1s), nothing else.");

            return promptBuilder.ToString();
        }

        private string ExtractJson(string response)
        {
            // Intentar extraer JSON si viene con texto adicional
            int jsonStart = response.IndexOf('[');
            int jsonEnd = response.LastIndexOf(']');

            if (jsonStart >= 0 && jsonEnd >= 0 && jsonEnd > jsonStart)
            {
                return response.Substring(jsonStart, jsonEnd - jsonStart + 1);
            }

            return response;
        }

        private string SerializePopulation(List<BinaryEvolutional> population)
        {
            var populationData = new List<string>();
            foreach (var individual in population)
            {
                populationData.Add(GetChromosomeString(individual));
            }
            return JsonSerializer.Serialize(populationData, new JsonSerializerOptions { WriteIndented = true });
        }

        private List<BinaryEvolutional> DeserializePopulation(string json, int size)
        {
            var populationData = JsonSerializer.Deserialize<List<string>>(json);
            var offspring = new List<BinaryEvolutional>();

            if (populationData == null)
                return offspring;

            foreach (var genes in populationData)
            {
                var individual = new BinaryEvolutional(size);
                for (int i = 0; i < Math.Min(genes.Length, size); i++)
                {
                    individual.SetGen(i, genes[i] == '1');
                }
                offspring.Add(individual);
            }

            return offspring;
        }
    }
}
