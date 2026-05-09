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

        /// <summary>
        /// Gets or sets the fitness function used to evaluate individuals.
        /// When set, fitness information is included in the AI prompt for better context.
        /// </summary>
        public Func<BinaryEvolutional, double>? FitnessFunction { get; set; }

        /// <summary>
        /// Creates a new AI-based crossover operator.
        /// </summary>
        /// <param name="aiProvider">The AI provider to use for crossover operations.</param>
        public AICrosser(IAIProvider aiProvider)
        {
            _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
        }

        public List<BinaryEvolutional> CrossPopulation(List<BinaryEvolutional> parents)
        {
            if (parents == null || parents.Count == 0)
                return new List<BinaryEvolutional>();

            _generationCount++;

            // 1. Calcular fitness si está disponible
            Dictionary<string, double>? fitnessMap = null;
            if (FitnessFunction != null)
            {
                fitnessMap = CalculateFitness(parents);
            }

            // 2. Serializar la población a JSON
            string serializedPopulation = SerializePopulation(parents, fitnessMap);

            // 3. Crear el prompt para la IA con contexto evolutivo
            string prompt = BuildPrompt(serializedPopulation, parents.Count, fitnessMap);

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

        private Dictionary<string, double> CalculateFitness(List<BinaryEvolutional> population)
        {
            var fitnessMap = new Dictionary<string, double>();
            var fitnessList = new List<double>();

            foreach (var individual in population)
            {
                double fitness = FitnessFunction!(individual);
                string key = GetChromosomeString(individual);
                fitnessMap[key] = fitness;
                fitnessList.Add(fitness);
            }

            _lastBestFitness = fitnessList.Max();
            _lastAverageFitness = fitnessList.Average();

            return fitnessMap;
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

        private string BuildPrompt(string populationJson, int expectedSize, Dictionary<string, double>? fitnessMap)
        {
            var promptBuilder = new StringBuilder();

            promptBuilder.AppendLine($"Generation {_generationCount}:");
            promptBuilder.AppendLine();

            if (fitnessMap != null)
            {
                promptBuilder.AppendLine($"Current population statistics:");
                promptBuilder.AppendLine($"- Best fitness: {_lastBestFitness:F2}");
                promptBuilder.AppendLine($"- Average fitness: {_lastAverageFitness:F2}");
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("Perform crossover to create offspring that:");
                promptBuilder.AppendLine("1. Preserve genetic material from high-fitness parents");
                promptBuilder.AppendLine("2. Explore new combinations that might improve fitness");
                promptBuilder.AppendLine("3. Maintain diversity in the population");
                promptBuilder.AppendLine();
            }
            else
            {
                promptBuilder.AppendLine("Perform genetic crossover operations to create diverse offspring.");
                promptBuilder.AppendLine();
            }

            promptBuilder.AppendLine($"Input population (with fitness scores):");
            promptBuilder.AppendLine(populationJson);
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

        private string SerializePopulation(List<BinaryEvolutional> population, Dictionary<string, double>? fitnessMap)
        {
            if (fitnessMap == null)
            {
                // Sin fitness, solo las cadenas binarias
                var populationData = new List<string>();
                foreach (var individual in population)
                {
                    populationData.Add(GetChromosomeString(individual));
                }
                return JsonSerializer.Serialize(populationData, new JsonSerializerOptions { WriteIndented = true });
            }
            else
            {
                // Con fitness, incluir información adicional
                var populationData = new List<object>();
                foreach (var individual in population)
                {
                    string chromosome = GetChromosomeString(individual);
                    populationData.Add(new
                    {
                        chromosome = chromosome,
                        fitness = fitnessMap[chromosome]
                    });
                }
                return JsonSerializer.Serialize(populationData, new JsonSerializerOptions { WriteIndented = true });
            }
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
