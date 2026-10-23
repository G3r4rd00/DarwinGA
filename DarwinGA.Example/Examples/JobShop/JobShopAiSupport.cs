using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DarwinGA.AI;
using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Evolutionals.BinaryEvolutional.Crossers;

namespace DarwinGA.Example
{
    internal sealed class JobShopAiSupport
    {
        private readonly JobShopProblem _problem;
        private readonly JobShopEncoding _encoding;
        private readonly IAIProvider _aiProvider;

        public JobShopAiSupport(JobShopProblem problem, JobShopEncoding encoding, IAIProvider aiProvider)
        {
            _problem = problem;
            _encoding = encoding;
            _aiProvider = aiProvider;
        }

        /// <summary>
        /// Creates the initial population mixing <paramref name="aiCount"/> AI-generated schedules with random individuals.
        /// </summary>
        public List<BinaryEvolutional> CreatePopulation(int populationSize, int aiCount)
        {
            aiCount = Math.Clamp(aiCount, 0, populationSize);
            var population = aiCount > 0 ? CreateAIPopulation(aiCount) : new List<BinaryEvolutional>();
            Console.WriteLine($"Initial population: {population.Count} AI + {populationSize - population.Count} random");

            population.AddRange(_encoding.CreateRandomPopulation(populationSize - population.Count));
            return population;
        }

        private List<BinaryEvolutional> CreateAIPopulation(int aiCount)
        {
            try
            {
                _aiProvider.SetSystemMessage(BuildPopulationSystemMessage(aiCount));

                var response = _aiProvider.SendPromptAsync($"Generate {aiCount} schedules.").GetAwaiter().GetResult();

                return ParseSequencesFromAiResponse(response, "AI initial population").Take(aiCount).ToList();
            }
            catch (Exception ex)
            {
                // If the AI fails the population is completed with random individuals.
                Console.WriteLine($"Error creating AI initial population: {ex.Message}");
                return new List<BinaryEvolutional>();
            }
        }

        /// <summary>
        /// Creates an AI crossover that generates <paramref name="offspringCount"/> children per generation;
        /// the genetic algorithm fills the rest with its classic crossover.
        /// </summary>
        public AICrosser CreateCrosser(int offspringCount)
        {
            return new AICrosser(
                _aiProvider,
                offspringCount,
                parents => BuildCrossoverPrompt(parents, offspringCount),
                response => ParseSequencesFromAiResponse(response, "AI crossover"),
                BuildCrossoverSystemMessage(offspringCount));
        }

        // Parents arrive sorted best first (elite selection); each one is sent with its makespan and machine timelines.
        private string BuildCrossoverPrompt(List<BinaryEvolutional> parents, int offspringCount)
        {
            var prompt = new StringBuilder("Parents (best first):\n");
            for (int i = 0; i < parents.Count; i++)
            {
                var schedule = _encoding.BuildSchedule(_encoding.Decode(parents[i]));
                prompt.AppendLine($"{i + 1}. makespan {schedule.Makespan}");
                prompt.AppendLine($"   sequence: {JsonSerializer.Serialize(schedule.ToSequence())}");
                foreach (var timeline in schedule.MachineTimelines())
                    prompt.AppendLine($"   {timeline}");
            }

            prompt.Append($"Generate {offspringCount} offspring sequences.");
            return prompt.ToString();
        }

        private string BuildPopulationSystemMessage(int count) => @$"
You generate the initial population of a genetic algorithm that solves a Job Shop Scheduling Problem.
{BuildProblemDefinition()}

TASK:
Create exactly {count} operation sequences with the lowest makespan you can find.
- Use dispatching rules such as: most work remaining first, shortest processing time, and give priority to the operations of the busiest (bottleneck) machines.
- Simulate the schedule to check the makespan and improve the sequence (swap adjacent operations on the critical path) whenever it is reduced.
- All sequences must be different from each other.
{BuildOutputRules(count)}";

        private string BuildCrossoverSystemMessage(int offspringCount) => @$"
You are the crossover operator of a genetic algorithm that solves a Job Shop Scheduling Problem.
{BuildProblemDefinition()}

TASK:
Each request lists the parent schedules, best first, with their makespan, their operation sequence and the resulting order of jobs on each machine.
Create exactly {offspringCount} offspring sequences.
- Combine the machine orders of the parents (e.g. keep the order of some machines from one parent and of the others from another), favouring the best parents.
- Then improve each child: find the critical path (the chain of operations without idle time that ends at the makespan) and swap adjacent operations of the same machine on it whenever it reduces the makespan.
- Every child must be different from all parents and from the other children.
{BuildOutputRules(offspringCount)}";

        private string BuildProblemDefinition()
        {
            var jobs = new StringBuilder();
            for (int job = 0; job < _problem.JobCount; job++)
            {
                var operations = Enumerable.Range(0, _problem.MachineCount)
                    .Select(k => $"M{_problem.Machines[job, k]}({_problem.Durations[job, k]})");
                jobs.AppendLine($"J{job}: {string.Join(" -> ", operations)}");
            }

            return @$"
PROBLEM DEFINITION:
- {_problem.JobCount} jobs (0 to {_problem.JobCount - 1}) and {_problem.MachineCount} machines (0 to {_problem.MachineCount - 1}).
- Every job is a chain of {_problem.MachineCount} operations that must run IN THE GIVEN ORDER, one after another; each operation needs one machine for a given time.
- A machine runs only one operation at a time and an operation cannot be interrupted.
- Makespan = time at which the last operation finishes. Lower is better. No schedule can be shorter than {_problem.LowerBound} (load of the busiest machine).

JOBS (operations in order, as Machine(duration)):
{jobs.ToString().TrimEnd()}

SOLUTION ENCODING (operation sequence):
- A list of {_problem.OperationCount} job numbers where every job appears exactly {_problem.MachineCount} times.
- The k-th appearance of job j means the k-th operation of job j.
- The schedule is built by reading the list from left to right: each operation starts as soon as its previous operation in the job has finished
  and its machine is free (it may use an earlier idle gap of the machine if it fits).";
        }

        private string BuildOutputRules(int count) => @$"
OUTPUT FORMAT:
- You may reason step by step first.
- End your answer with exactly one ```json block containing only a JSON array of exactly {count} sequences, e.g. [[0,2,1,0,...],[1,0,2,...]].
- Each sequence has exactly {_problem.OperationCount} numbers and every job 0..{_problem.JobCount - 1} appears exactly {_problem.MachineCount} times. Check every sequence before answering.";

        private List<BinaryEvolutional> ParseSequencesFromAiResponse(string response, string source)
        {
            try
            {
                // Only the last ```json block is parsed when present, so numbers in the reasoning are ignored.
                var jsonBlocks = Regex.Matches(response, @"```json\s*(.*?)```", RegexOptions.Singleline);
                string answer = jsonBlocks.Count > 0 ? jsonBlocks[^1].Groups[1].Value : response;

                var rawSequences = Regex.Matches(answer, @"\[\s*([^\[\]]+)\s*\]")
                    .Select(match => Regex.Matches(match.Groups[1].Value, @"-?\d+")
                        .Select(number => int.Parse(number.Value, CultureInfo.InvariantCulture))
                        .ToList())
                    .Where(numbers => numbers.Count > 0)
                    .ToList();

                var sequences = rawSequences
                    .Select(_encoding.RepairSequence)
                    .Where(sequence => sequence is not null)
                    .Cast<List<int>>()
                    .ToList();

                int exact = rawSequences.Count(IsValidSequence);
                string bestMakespan = sequences.Count > 0
                    ? sequences.Min(sequence => _encoding.BuildSchedule(sequence).Makespan).ToString(CultureInfo.InvariantCulture)
                    : "-";
                Console.WriteLine($"{source}: {sequences.Count}/{rawSequences.Count} usable sequences ({exact} valid as returned), best makespan {bestMakespan}");

                return sequences.Select(_encoding.Encode).ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("AI returned a response that could not be parsed.", ex);
            }
        }

        private bool IsValidSequence(List<int> sequence)
            => sequence.Count == _problem.OperationCount
               && sequence.All(job => job >= 0 && job < _problem.JobCount)
               && sequence.GroupBy(job => job).All(group => group.Count() == _problem.MachineCount)
               && sequence.Distinct().Count() == _problem.JobCount;
    }
}
