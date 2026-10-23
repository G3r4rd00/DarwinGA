namespace DarwinGA.Example
{
    /// <summary>
    /// Job Shop Scheduling Problem (JSSP): every job is a fixed sequence of operations, each one needs a given
    /// machine for a given time. Each machine handles one operation at a time; the goal is the schedule with
    /// the shortest makespan (time at which the last operation finishes).
    /// Unlike the TSP, the quality of a schedule cannot be read from local "cheap edges": the makespan depends
    /// on how every machine queue interacts with every job, so it is very hard to solve in one shot.
    /// </summary>
    internal sealed class JobShopProblem
    {
        public string Name { get; }
        public int JobCount { get; }
        public int MachineCount { get; }
        public int OperationCount => JobCount * MachineCount;

        // Machine and duration of the k-th operation of each job: Machines[job, k], Durations[job, k].
        public int[,] Machines { get; }
        public int[,] Durations { get; }

        // Best known (proven optimal) makespan of the benchmark instance.
        public int OptimalMakespan { get; }

        // Trivial lower bound: the busiest machine or the longest job.
        public int LowerBound { get; }

        private JobShopProblem(string name, int[][] rows, int optimalMakespan)
        {
            Name = name;
            JobCount = rows.Length;
            MachineCount = rows[0].Length / 2;
            OptimalMakespan = optimalMakespan;
            Machines = new int[JobCount, MachineCount];
            Durations = new int[JobCount, MachineCount];

            for (int job = 0; job < JobCount; job++)
            {
                for (int k = 0; k < MachineCount; k++)
                {
                    Machines[job, k] = rows[job][2 * k];
                    Durations[job, k] = rows[job][2 * k + 1];
                }
            }

            LowerBound = ComputeLowerBound();
        }

        /// <summary>
        /// Fisher &amp; Thompson 6x6 instance (ft06). Optimum makespan 55.
        /// </summary>
        public static JobShopProblem FT06() => Shuffled("FT06 (6 jobs x 6 machines)", new[]
        {
            new[] { 2, 1, 0, 3, 1, 6, 3, 7, 5, 3, 4, 6 },
            new[] { 1, 8, 2, 5, 4, 10, 5, 10, 0, 10, 3, 4 },
            new[] { 2, 5, 3, 4, 5, 8, 0, 9, 1, 1, 4, 7 },
            new[] { 1, 5, 0, 5, 2, 5, 3, 3, 4, 8, 5, 9 },
            new[] { 2, 9, 1, 3, 4, 5, 5, 4, 0, 3, 3, 1 },
            new[] { 1, 3, 3, 3, 5, 9, 0, 10, 4, 4, 2, 1 },
        }, optimalMakespan: 55);

        /// <summary>
        /// Fisher &amp; Thompson 10x10 instance (ft10/mt10). Optimum makespan 930, it stayed unsolved from 1963 to 1989.
        /// </summary>
        public static JobShopProblem FT10() => Shuffled("FT10 (10 jobs x 10 machines)", new[]
        {
            new[] { 0, 29, 1, 78, 2, 9, 3, 36, 4, 49, 5, 11, 6, 62, 7, 56, 8, 44, 9, 21 },
            new[] { 0, 43, 2, 90, 4, 75, 9, 11, 3, 69, 1, 28, 6, 46, 5, 46, 7, 72, 8, 30 },
            new[] { 1, 91, 0, 85, 3, 39, 2, 74, 8, 90, 5, 10, 7, 12, 6, 89, 9, 45, 4, 33 },
            new[] { 1, 81, 2, 95, 0, 71, 4, 99, 6, 9, 8, 52, 7, 85, 3, 98, 9, 22, 5, 43 },
            new[] { 2, 14, 0, 6, 1, 22, 5, 61, 3, 26, 4, 69, 8, 21, 7, 49, 9, 72, 6, 53 },
            new[] { 2, 84, 1, 2, 5, 52, 3, 95, 8, 48, 9, 72, 0, 47, 6, 65, 4, 6, 7, 25 },
            new[] { 1, 46, 0, 37, 3, 61, 2, 13, 6, 32, 5, 21, 9, 32, 8, 89, 7, 30, 4, 55 },
            new[] { 2, 31, 0, 86, 1, 46, 5, 74, 4, 32, 6, 88, 8, 19, 9, 48, 7, 36, 3, 79 },
            new[] { 0, 76, 1, 69, 3, 76, 5, 51, 2, 85, 9, 11, 6, 40, 7, 89, 4, 26, 8, 74 },
            new[] { 1, 85, 0, 13, 2, 61, 6, 7, 8, 64, 9, 49, 3, 76, 5, 47, 4, 52, 7, 90 },
        }, optimalMakespan: 930);

        // Jobs and machines are relabeled at random on every run: the optimum does not change, but the instance
        // cannot be recognised as the published benchmark (and its published solution cannot be recalled).
        private static JobShopProblem Shuffled(string name, int[][] rows, int optimalMakespan)
        {
            int machineCount = rows[0].Length / 2;
            var machineLabel = Enumerable.Range(0, machineCount).OrderBy(_ => MyRandom.NextInt(int.MaxValue)).ToArray();

            var shuffledRows = rows
                .OrderBy(_ => MyRandom.NextInt(int.MaxValue))
                .Select(row => row.Select((value, i) => i % 2 == 0 ? machineLabel[value] : value).ToArray())
                .ToArray();

            return new JobShopProblem(name, shuffledRows, optimalMakespan);
        }

        private int ComputeLowerBound()
        {
            var machineLoad = new int[MachineCount];
            int longestJob = 0;

            for (int job = 0; job < JobCount; job++)
            {
                int jobLength = 0;
                for (int k = 0; k < MachineCount; k++)
                {
                    machineLoad[Machines[job, k]] += Durations[job, k];
                    jobLength += Durations[job, k];
                }

                longestJob = Math.Max(longestJob, jobLength);
            }

            return Math.Max(machineLoad.Max(), longestJob);
        }
    }
}
