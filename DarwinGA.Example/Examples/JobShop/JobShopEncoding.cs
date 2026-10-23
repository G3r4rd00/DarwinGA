using DarwinGA.Evolutionals.BinaryEvolutional;

namespace DarwinGA.Example
{
    /// <summary>
    /// Random-key encoding: every operation slot has a binary key. Slots sorted by key give an operation sequence
    /// (slot s belongs to job s / MachineCount), where the k-th appearance of a job is its k-th operation.
    /// Any bit string decodes to a valid sequence, so the classic bit crossover and mutation never break it.
    /// </summary>
    internal sealed class JobShopEncoding
    {
        private const int MinBitsPerKey = 8;

        private readonly JobShopProblem _problem;

        public int BitsPerKey { get; }
        public int ChromosomeSize => _problem.OperationCount * BitsPerKey;

        public JobShopEncoding(JobShopProblem problem)
        {
            _problem = problem;
            // Enough bits to give every slot its own rank when encoding a sequence.
            BitsPerKey = Math.Max(MinBitsPerKey, (int)Math.Ceiling(Math.Log2(problem.OperationCount)));
        }

        public List<BinaryEvolutional> CreateRandomPopulation(int populationSize)
        {
            var population = new List<BinaryEvolutional>();

            for (int p = 0; p < populationSize; p++)
            {
                var chromosome = new BinaryEvolutional(ChromosomeSize);
                for (int i = 0; i < ChromosomeSize; i++)
                    chromosome.SetGen(i, MyRandom.NextBool());

                population.Add(chromosome);
            }

            return population;
        }

        // 1.0 when the target makespan (optimum or lower bound) is reached.
        public double EvaluateFitness(BinaryEvolutional chromosome)
            => _problem.TargetMakespan / (double)BuildSchedule(Decode(chromosome)).Makespan;

        public List<int> Decode(BinaryEvolutional chromosome)
        {
            var keys = new int[_problem.OperationCount];

            for (int slot = 0; slot < keys.Length; slot++)
            {
                int key = 0;
                for (int b = 0; b < BitsPerKey; b++)
                {
                    if (chromosome.GetGen(slot * BitsPerKey + b))
                        key |= 1 << (BitsPerKey - 1 - b);
                }

                keys[slot] = key;
            }

            // Ties are broken by slot index so decoding is deterministic.
            return Enumerable.Range(0, keys.Length)
                .OrderBy(slot => keys[slot])
                .ThenBy(slot => slot)
                .Select(slot => slot / _problem.MachineCount)
                .ToList();
        }

        public BinaryEvolutional Encode(IReadOnlyList<int> sequence)
        {
            var genes = new bool[ChromosomeSize];
            var nextSlotOfJob = new int[_problem.JobCount];

            // The k-th appearance of a job takes the k-th slot of that job and gets its position as key.
            for (int position = 0; position < sequence.Count; position++)
            {
                int job = sequence[position];
                int slot = job * _problem.MachineCount + nextSlotOfJob[job]++;

                for (int b = 0; b < BitsPerKey; b++)
                    genes[slot * BitsPerKey + b] = (position & (1 << (BitsPerKey - 1 - b))) != 0;
            }

            return new BinaryEvolutional(genes);
        }

        /// <summary>
        /// Builds the schedule of an operation sequence: each operation starts as soon as its job and machine allow,
        /// filling earlier idle gaps of the machine when it fits (active schedule).
        /// </summary>
        public JobShopSchedule BuildSchedule(IReadOnlyList<int> sequence)
        {
            var start = new int[_problem.JobCount, _problem.MachineCount];
            var nextOperation = new int[_problem.JobCount];
            var jobReady = new int[_problem.JobCount];
            // Busy intervals of each machine, sorted by start time.
            var machineBusy = Enumerable.Range(0, _problem.MachineCount).Select(_ => new List<(int Start, int End)>()).ToArray();
            int makespan = 0;

            foreach (int job in sequence)
            {
                int k = nextOperation[job]++;
                int machine = _problem.Machines[job, k];
                int duration = _problem.Durations[job, k];
                var busy = machineBusy[machine];

                int candidate = jobReady[job];
                int insertAt = busy.Count;
                for (int i = 0; i < busy.Count; i++)
                {
                    if (candidate + duration <= busy[i].Start)
                    {
                        insertAt = i;
                        break;
                    }

                    candidate = Math.Max(candidate, busy[i].End);
                }

                busy.Insert(insertAt, (candidate, candidate + duration));
                start[job, k] = candidate;
                jobReady[job] = candidate + duration;
                makespan = Math.Max(makespan, jobReady[job]);
            }

            return new JobShopSchedule(_problem, start, makespan);
        }

        /// <summary>
        /// Keeps the first MachineCount appearances of every valid job and appends the missing ones, so a slightly
        /// wrong sequence (e.g. miscounted by the AI) still becomes a valid one. Returns null if it is unusable.
        /// </summary>
        public List<int>? RepairSequence(IEnumerable<int> rawSequence)
        {
            var count = new int[_problem.JobCount];
            var sequence = new List<int>();

            foreach (int job in rawSequence)
            {
                if (job < 0 || job >= _problem.JobCount || count[job] >= _problem.MachineCount)
                    continue;

                count[job]++;
                sequence.Add(job);
            }

            if (sequence.Count < _problem.OperationCount / 2)
                return null;

            for (int job = 0; job < _problem.JobCount; job++)
                for (; count[job] < _problem.MachineCount; count[job]++)
                    sequence.Add(job);

            return sequence;
        }
    }

    internal sealed class JobShopSchedule
    {
        private readonly JobShopProblem _problem;

        // Start time of the k-th operation of each job.
        public int[,] Start { get; }
        public int Makespan { get; }

        public JobShopSchedule(JobShopProblem problem, int[,] start, int makespan)
        {
            _problem = problem;
            Start = start;
            Makespan = makespan;
        }

        /// <summary>
        /// The operations sorted by start time: decoding this sequence rebuilds exactly this schedule, so it is the
        /// representation shown to the AI.
        /// </summary>
        public List<int> ToSequence()
            => Operations()
                .OrderBy(op => op.Start)
                .ThenBy(op => op.Job)
                .Select(op => op.Job)
                .ToList();

        // One line per machine: "M0: J3[0-29] J1[29-72] ...".
        public IEnumerable<string> MachineTimelines()
            => Enumerable.Range(0, _problem.MachineCount).Select(machine =>
                $"M{machine}: " + string.Join(" ", Operations()
                    .Where(op => _problem.Machines[op.Job, op.Index] == machine)
                    .OrderBy(op => op.Start)
                    .Select(op => $"J{op.Job}[{op.Start}-{op.Start + _problem.Durations[op.Job, op.Index]}]")));

        private IEnumerable<(int Job, int Index, int Start)> Operations()
        {
            for (int job = 0; job < _problem.JobCount; job++)
                for (int k = 0; k < _problem.MachineCount; k++)
                    yield return (job, k, Start[job, k]);
        }
    }
}
