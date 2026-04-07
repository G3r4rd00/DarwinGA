using DarwinGA;
using DarwinGA.Diversity;
using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Evolutionals.BinaryEvolutional.Crossers;
using DarwinGA.Evolutionals.BinaryEvolutional.Mutations;
using DarwinGA.Interfaces;
using DarwinGA.Selections;
using DarwinGA.Terminations;
using System.Globalization;
using System.Drawing.Drawing2D;

namespace DarwinGA.WinFormsUI;

public partial class Form1 : Form
{
    private readonly record struct KnapsackItem(int Weight, double Value);

    private readonly KnapsackItem[] _items =
    [
        new(12, 60), new(7, 34), new(11, 55), new(8, 40), new(9, 42), new(6, 30), new(13, 70), new(5, 25),
        new(14, 76), new(4, 18), new(10, 50), new(3, 14), new(15, 82), new(2, 10), new(16, 90), new(1, 6)
    ];

    private readonly int[] _partitionNumbers = [8, 13, 27, 6, 19, 41, 12, 7, 24, 35, 3, 16, 28, 9, 22, 14];
    private readonly (int A, int B)[] _maxCutEdges =
    [
        (0, 1), (0, 2), (0, 5), (1, 2), (1, 3), (2, 4), (2, 6), (3, 4), (3, 7), (4, 5),
        (4, 8), (5, 6), (5, 9), (6, 7), (6, 10), (7, 8), (7, 11), (8, 9), (9, 10), (10, 11),
        (11, 0), (1, 9), (2, 8), (3, 10)
    ];
    private const int MaxCutNodeCount = 12;

    private ComboBox _problemCombo = null!;
    private ComboBox _selectionCombo = null!;
    private ComboBox _crossCombo = null!;
    private ComboBox _mutationCombo = null!;
    private NumericUpDown _populationNum = null!;
    private NumericUpDown _generationsNum = null!;
    private NumericUpDown _geneCountNum = null!;
    private NumericUpDown _capacityNum = null!;
    private NumericUpDown _mutationProbNum = null!;
    private NumericUpDown _diversityPenaltyNum = null!;
    private NumericUpDown _selectionFractionNum = null!;
    private NumericUpDown _tournamentKNum = null!;
    private CheckBox _enableDiversityCheck = null!;
    private CheckBox _enableParallelEvalCheck = null!;
    private CheckBox _enableParallelBreedCheck = null!;
    private Button _runButton = null!;
    private Button _stopButton = null!;
    private SimpleLineChart _fitnessChart = null!;
    private SimpleLineChart _populationChart = null!;
    private ListBox _logList = null!;
    private ToolTip _toolTip = null!;

    private CancellationTokenSource? _cts;
    private GeneticAlgorithm<BinaryEvolutional>? _runningGa;
    private int _hotCapacity;

    private static readonly IReadOnlyDictionary<string, string> ProblemDescriptions = new Dictionary<string, string>
    {
        ["OneMax"] = "Maximizes the number of 1 bits in the chromosome.",
        ["DeceptiveTrap"] = "Deceptive block problem: appears to improve but gets trapped in local optima before reaching the global optimum.",
        ["RoyalRoad"] = "Block-based function: grants full fitness only when entire blocks are all 1s.",
        ["NKLandscape"] = "Rugged fitness landscape with gene epistasis (N-K model), containing many local optima.",
        ["LeadingOnes"] = "Maximizes the number of consecutive ones from the start of the chromosome.",
        ["TargetPattern"] = "Finds a chromosome that matches a predefined target bit pattern.",
        ["Knapsack"] = "Selects items to maximize value without exceeding knapsack capacity.",
        ["Partition"] = "Splits a set of numbers into two subsets with sums as balanced as possible.",
        ["MaxCut"] = "Partitions graph nodes into two sets maximizing the number of crossing edges."
    };

    public Form1()
    {
        InitializeComponent();
        BuildUi();
        PopulateOptions();
    }

    private void BuildUi()
    {
        Text = "DarwinGA - Genetic Algorithm Lab";
        Width = 1350;
        Height = 850;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 65));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 35));

        var optionsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,
            Padding = new Padding(8)
        };

        _problemCombo = CreateCombo(130);
        _toolTip = new ToolTip();
        _selectionCombo = CreateCombo(140);
        _crossCombo = CreateCombo(140);
        _mutationCombo = CreateCombo(160);
        _populationNum = CreateNum(30, 2000, 150, 0);
        _generationsNum = CreateNum(1, 5000, 200, 0);
        _geneCountNum = CreateNum(8, 2048, 512, 0);
        _capacityNum = CreateNum(1, 500, 60, 0);
        _mutationProbNum = CreateNum(0, 1, 0.06M, 2);
        _diversityPenaltyNum = CreateNum(0, 5, 0.6M, 2);
        _selectionFractionNum = CreateNum(0.05M, 1, 0.35M, 2);
        _tournamentKNum = CreateNum(2, 100, 3, 0);

        _enableDiversityCheck = new CheckBox { Text = "Enable diversity", Checked = true, AutoSize = true };
        _enableParallelEvalCheck = new CheckBox { Text = "Parallel evaluation", Checked = true, AutoSize = true };
        _enableParallelBreedCheck = new CheckBox { Text = "Parallel breeding", Checked = true, AutoSize = true };
        _runButton = new Button { Text = "Start", Width = 120, Height = 30 };
        _stopButton = new Button { Text = "Stop", Width = 120, Height = 30, Enabled = false };

        _runButton.Click += RunButton_Click;
        _stopButton.Click += StopButton_Click;

        optionsPanel.Controls.AddRange(
        [
            CreateLabeled("Problem", _problemCombo),
            CreateLabeled("Selection", _selectionCombo),
            CreateLabeled("Selection fraction", _selectionFractionNum),
            CreateLabeled("Tournament K", _tournamentKNum),
            CreateLabeled("Crossover", _crossCombo),
            CreateLabeled("Mutation", _mutationCombo),
            CreateLabeled("Mutation prob.", _mutationProbNum),
            CreateLabeled("Population", _populationNum),
            CreateLabeled("Generations", _generationsNum),
            CreateLabeled("Genes (binary)", _geneCountNum),
            CreateLabeled("Capacity (Knapsack)", _capacityNum),
            CreateLabeled("Diversity factor", _diversityPenaltyNum),
            _enableDiversityCheck,
            _enableParallelEvalCheck,
            _enableParallelBreedCheck,
            _runButton,
            _stopButton
        ]);

        var chartsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        chartsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
        chartsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));

        _fitnessChart = CreateChart("Fitness evolution", ["Best", "Avg", "Min", "Max", "Diversity"]);
        _populationChart = CreateChart("Population indicators", ["BestOnes(%)", "StdDev"]);
        chartsPanel.Controls.Add(_fitnessChart, 0, 0);
        chartsPanel.Controls.Add(_populationChart, 1, 0);

        _logList = new ListBox { Dock = DockStyle.Fill, HorizontalScrollbar = true };

        root.Controls.Add(optionsPanel, 0, 0);
        root.Controls.Add(chartsPanel, 0, 1);
        root.Controls.Add(_logList, 0, 2);

        Controls.Add(root);

        _problemCombo.SelectedIndexChanged += (_, _) =>
        {
            var problem = _problemCombo.SelectedItem?.ToString() ?? "OneMax";
            bool usesGeneCount = problem is "OneMax" or "DeceptiveTrap" or "RoyalRoad" or "NKLandscape" or "LeadingOnes" or "TargetPattern";
            _geneCountNum.Enabled = usesGeneCount;
            _capacityNum.Enabled = problem == "Knapsack";
            UpdateProblemTooltip(problem);
        };

        _selectionCombo.SelectedIndexChanged += (_, _) => ApplyHotReloadParameters();
        _crossCombo.SelectedIndexChanged += (_, _) => ApplyHotReloadParameters();
        _mutationCombo.SelectedIndexChanged += (_, _) => ApplyHotReloadParameters();
        _generationsNum.ValueChanged += (_, _) => ApplyHotReloadParameters();
        _capacityNum.ValueChanged += (_, _) => ApplyHotReloadParameters();
        _mutationProbNum.ValueChanged += (_, _) => ApplyHotReloadParameters();
        _diversityPenaltyNum.ValueChanged += (_, _) => ApplyHotReloadParameters();
        _selectionFractionNum.ValueChanged += (_, _) => ApplyHotReloadParameters();
        _tournamentKNum.ValueChanged += (_, _) => ApplyHotReloadParameters();
        _enableDiversityCheck.CheckedChanged += (_, _) => ApplyHotReloadParameters();
        _enableParallelEvalCheck.CheckedChanged += (_, _) => ApplyHotReloadParameters();
        _enableParallelBreedCheck.CheckedChanged += (_, _) => ApplyHotReloadParameters();
    }

    private void PopulateOptions()
    {
        _problemCombo.Items.AddRange(["DeceptiveTrap", "RoyalRoad", "NKLandscape", "OneMax", "LeadingOnes", "TargetPattern", "Knapsack", "Partition", "MaxCut"]);
        _problemCombo.SelectedIndex = 0;
        UpdateProblemTooltip("DeceptiveTrap");

        _selectionCombo.Items.AddRange(["Tournament", "RouletteWheel", "Rank", "Truncation", "SUS", "Elite"]);
        _selectionCombo.SelectedIndex = 0;

        _crossCombo.Items.AddRange(["OnePoint", "TwoPoint", "Uniform", "NPoint", "HUX", "Arithmetic", "Partial", "SegmentSwap"]);
        _crossCombo.SelectedIndex = 2;

        _mutationCombo.Items.AddRange([
            "Random", "KFlip", "BlockFlip", "MultiBlockFlip", "RunFlip", "SwapPairs", "BitMask", "ShiftRotation", "NonUniform", "GeometricBlock", "Scramble"
        ]);
        _mutationCombo.SelectedIndex = 0;
    }

    private async void RunButton_Click(object? sender, EventArgs e)
    {
        if (_cts != null)
            return;

        var settings = ReadSettings();
        ClearOutputs();
        ToggleRunState(isRunning: true);

        _cts = new CancellationTokenSource();

        try
        {
            await Task.Run(() => Execute(settings, _cts.Token));
            _logList.Items.Add("Completed.");
        }
        catch (OperationCanceledException)
        {
            _logList.Items.Add("Execution canceled by user.");
        }
        catch (Exception ex)
        {
            _logList.Items.Add($"Error: {ex.Message}");
        }
        finally
        {
            _runningGa = null;
            _cts.Dispose();
            _cts = null;
            ToggleRunState(isRunning: false);
        }
    }

    private void StopButton_Click(object? sender, EventArgs e)
    {
        _cts?.Cancel();
    }

    private void Execute(GaSettings s, CancellationToken token)
    {
        int chromosomeSize = s.Problem switch
        {
            "DeceptiveTrap" => s.GeneCount,
            "RoyalRoad" => s.GeneCount,
            "NKLandscape" => s.GeneCount,
            "OneMax" => s.GeneCount,
            "LeadingOnes" => s.GeneCount,
            "TargetPattern" => s.GeneCount,
            "Knapsack" => _items.Length,
            "Partition" => _partitionNumbers.Length,
            "MaxCut" => MaxCutNodeCount,
            _ => s.GeneCount
        };
        _hotCapacity = s.Capacity;

        Func<BinaryEvolutional> newItem = () =>
        {
            var chr = new BinaryEvolutional(chromosomeSize);
            for (int i = 0; i < chromosomeSize; i++)
                chr.SetGen(i, MyRandom.NextDouble() < 0.5);
            return chr;
        };

        Func<BinaryEvolutional, double> fitness = s.Problem switch
        {
            "DeceptiveTrap" => DeceptiveTrapFitness,
            "RoyalRoad" => RoyalRoadFitness,
            "NKLandscape" => NKLandscapeFitness,
            "OneMax" => OneMaxFitness,
            "LeadingOnes" => LeadingOnesFitness,
            "TargetPattern" => TargetPatternFitness,
            "Knapsack" => e => KnapsackFitness(e, Volatile.Read(ref _hotCapacity)),
            "Partition" => PartitionFitness,
            "MaxCut" => MaxCutFitness,
            _ => OneMaxFitness
        };

        var ga = new GeneticAlgorithm<BinaryEvolutional>
        {
            NewItem = newItem,
            Fitness = fitness,
            MutationProbability = s.MutationProbability,
            Mutation = CreateMutation(s.MutationType),
            Cross = CreateCross(s.CrossType),
            Selection = CreateSelection(s.SelectionType, s.SelectionFraction, s.TournamentK),
            Termination = new GenerationNumTermination(s.Generations),
            EnableParallelEvaluation = s.ParallelEval,
            EnableParallelBreeding = s.ParallelBreed,
            EnableDiversity = s.EnableDiversity,
            DiversityMetric = s.EnableDiversity
                ? new DelegateDiversityMetric<BinaryEvolutional>(HammingDistance)
                : null,
            DiversityStrategy = s.EnableDiversity
                ? new SimilarityPenaltyStrategy<BinaryEvolutional>(s.DiversityPenalty)
                : null,
            OnNewGeneration = result => SafeUiUpdate(() => OnGeneration(result, s))
        };

        _runningGa = ga;

        ga.Run(s.PopulationSize, token);
    }

    private void ApplyHotReloadParameters()
    {
        var ga = _runningGa;
        if (ga is null)
            return;

        _hotCapacity = (int)_capacityNum.Value;
        ga.MutationProbability = (double)_mutationProbNum.Value;
        ga.EnableParallelEvaluation = _enableParallelEvalCheck.Checked;
        ga.EnableParallelBreeding = _enableParallelBreedCheck.Checked;

        ga.Cross = CreateCross(_crossCombo.SelectedItem?.ToString() ?? "Uniform");
        ga.Mutation = CreateMutation(_mutationCombo.SelectedItem?.ToString() ?? "Random");
        ga.Selection = CreateSelection(
            _selectionCombo.SelectedItem?.ToString() ?? "Tournament",
            (double)_selectionFractionNum.Value,
            (int)_tournamentKNum.Value);
        ga.Termination = new GenerationNumTermination((int)_generationsNum.Value);

        bool enableDiversity = _enableDiversityCheck.Checked;
        ga.EnableDiversity = enableDiversity;
        ga.DiversityMetric = enableDiversity
            ? new DelegateDiversityMetric<BinaryEvolutional>(HammingDistance)
            : null;
        ga.DiversityStrategy = enableDiversity
            ? new SimilarityPenaltyStrategy<BinaryEvolutional>((double)_diversityPenaltyNum.Value)
            : null;

        _logList.Items.Add("[HotReload] Runtime parameters applied.");
        if (_logList.Items.Count > 600)
            _logList.Items.RemoveAt(0);
        _logList.TopIndex = _logList.Items.Count - 1;
    }

    private void OnGeneration(GenerationResult<BinaryEvolutional> result, GaSettings settings)
    {
        _fitnessChart.AddPoint("Best", result.GenerationNum, result.BestFitness);
        _fitnessChart.AddPoint("Avg", result.GenerationNum, result.AverageFitness);
        _fitnessChart.AddPoint("Min", result.GenerationNum, result.MinFitness);
        _fitnessChart.AddPoint("Max", result.GenerationNum, result.MaxFitness);
        _fitnessChart.AddPoint("Diversity", result.GenerationNum, result.DiversityIndex);

        double onesRatio = result.BestElement.Size == 0
            ? 0
            : CountOnes(result.BestElement) * 100.0 / result.BestElement.Size;

        _populationChart.AddPoint("BestOnes(%)", result.GenerationNum, onesRatio);
        _populationChart.AddPoint("StdDev", result.GenerationNum, result.FitnessStdDev);

        string extra = settings.Problem switch
        {
            "Knapsack" => GetKnapsackInfo(result.BestElement, Volatile.Read(ref _hotCapacity)),
            "Partition" => GetPartitionInfo(result.BestElement),
            "MaxCut" => GetMaxCutInfo(result.BestElement),
            "DeceptiveTrap" => $"TrapScore={DeceptiveTrapFitness(result.BestElement):F2}",
            "RoyalRoad" => $"RoyalRoad={RoyalRoadFitness(result.BestElement):F2}",
            "NKLandscape" => $"NKScore={NKLandscapeFitness(result.BestElement):F2}",
            "LeadingOnes" => $"LeadingOnes={LeadingOnesFitness(result.BestElement):F0}/{result.BestElement.Size}",
            "TargetPattern" => $"Matches={TargetPatternFitness(result.BestElement):F0}/{result.BestElement.Size}",
            _ => $"Ones={CountOnes(result.BestElement)}/{result.BestElement.Size}"
        };

        _logList.Items.Add(
            string.Format(
                CultureInfo.InvariantCulture,
                "Gen {0,4} | Best {1,9:F2} | Avg {2,9:F2} | Std {3,8:F2} | Div {4,6:F2} | {5}",
                result.GenerationNum,
                result.BestFitness,
                result.AverageFitness,
                result.FitnessStdDev,
                result.DiversityIndex,
                extra));

        if (_logList.Items.Count > 600)
            _logList.Items.RemoveAt(0);

        _logList.TopIndex = _logList.Items.Count - 1;
    }

    private static double OneMaxFitness(BinaryEvolutional e) => CountOnes(e);

    private static double DeceptiveTrapFitness(BinaryEvolutional e)
    {
        const int blockSize = 5;
        double fitness = 0;

        for (int start = 0; start < e.Size; start += blockSize)
        {
            int len = Math.Min(blockSize, e.Size - start);
            int ones = 0;
            for (int i = 0; i < len; i++)
            {
                if (e.GetGen(start + i))
                    ones++;
            }

            fitness += ones == len ? len : (len - 1 - ones);
        }

        return fitness;
    }

    private static double RoyalRoadFitness(BinaryEvolutional e)
    {
        const int blockSize = 8;
        double fitness = 0;

        for (int start = 0; start < e.Size; start += blockSize)
        {
            int len = Math.Min(blockSize, e.Size - start);
            bool fullOnes = true;
            for (int i = 0; i < len; i++)
            {
                if (!e.GetGen(start + i))
                {
                    fullOnes = false;
                    break;
                }
            }

            if (fullOnes)
                fitness += len;
        }

        return fitness;
    }

    private static double NKLandscapeFitness(BinaryEvolutional e)
    {
        const int k = 4;
        int n = e.Size;
        if (n == 0)
            return 0;

        double sum = 0;
        for (int i = 0; i < n; i++)
        {
            int pattern = 0;
            for (int j = 0; j <= k; j++)
            {
                int idx = (i + j) % n;
                if (e.GetGen(idx))
                    pattern |= 1 << j;
            }

            sum += DeterministicContribution(i, pattern);
        }

        return sum / n;
    }

    private static double DeterministicContribution(int geneIndex, int pattern)
    {
        uint x = (uint)(geneIndex * 73856093) ^ (uint)(pattern * 19349663) ^ 83492791u;
        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;
        return (x % 10000) / 10000.0;
    }

    private static double LeadingOnesFitness(BinaryEvolutional e)
    {
        int c = 0;
        for (int i = 0; i < e.Size; i++)
        {
            if (!e.GetGen(i))
                break;
            c++;
        }

        return c;
    }

    private static double TargetPatternFitness(BinaryEvolutional e)
    {
        int matches = 0;
        for (int i = 0; i < e.Size; i++)
        {
            if (e.GetGen(i) == GetTargetBit(i))
                matches++;
        }

        return matches;
    }

    private static bool GetTargetBit(int index)
        => (index % 2 == 0) ^ (index % 5 == 0);

    private double KnapsackFitness(BinaryEvolutional e, int capacity)
    {
        int weight = 0;
        double value = 0;

        for (int i = 0; i < e.Size; i++)
        {
            if (!e.GetGen(i))
                continue;

            weight += _items[i].Weight;
            value += _items[i].Value;
        }

        if (weight <= capacity)
            return value;

        int extra = weight - capacity;
        return value - (extra * extra * 5.0);
    }

    private double PartitionFitness(BinaryEvolutional e)
    {
        int sumA = 0;
        int sumB = 0;
        for (int i = 0; i < e.Size; i++)
        {
            if (e.GetGen(i))
                sumA += _partitionNumbers[i];
            else
                sumB += _partitionNumbers[i];
        }

        int total = sumA + sumB;
        int diff = Math.Abs(sumA - sumB);
        return total - diff;
    }

    private double MaxCutFitness(BinaryEvolutional e)
    {
        int crossing = 0;
        for (int i = 0; i < _maxCutEdges.Length; i++)
        {
            var edge = _maxCutEdges[i];
            if (e.GetGen(edge.A) != e.GetGen(edge.B))
                crossing++;
        }

        return crossing;
    }

    private string GetKnapsackInfo(BinaryEvolutional e, int capacity)
    {
        int weight = 0;
        double value = 0;
        int selected = 0;

        for (int i = 0; i < e.Size; i++)
        {
            if (!e.GetGen(i))
                continue;

            selected++;
            weight += _items[i].Weight;
            value += _items[i].Value;
        }

        return $"Weight={weight}/{capacity}, Value={value:F1}, Items={selected}";
    }

    private string GetPartitionInfo(BinaryEvolutional e)
    {
        int sumA = 0;
        int sumB = 0;
        for (int i = 0; i < e.Size; i++)
        {
            if (e.GetGen(i))
                sumA += _partitionNumbers[i];
            else
                sumB += _partitionNumbers[i];
        }

        return $"SetA={sumA}, SetB={sumB}, Diff={Math.Abs(sumA - sumB)}";
    }

    private string GetMaxCutInfo(BinaryEvolutional e)
    {
        int crossing = 0;
        for (int i = 0; i < _maxCutEdges.Length; i++)
        {
            var edge = _maxCutEdges[i];
            if (e.GetGen(edge.A) != e.GetGen(edge.B))
                crossing++;
        }

        return $"CrossingEdges={crossing}/{_maxCutEdges.Length}";
    }

    private static int CountOnes(BinaryEvolutional e)
    {
        int c = 0;
        for (int i = 0; i < e.Size; i++)
            if (e.GetGen(i)) c++;
        return c;
    }

    private static double HammingDistance(BinaryEvolutional a, BinaryEvolutional b)
    {
        int diff = 0;
        for (int i = 0; i < a.Size; i++)
            if (a.GetGen(i) != b.GetGen(i)) diff++;
        return diff;
    }

    private static ICross<BinaryEvolutional> CreateCross(string name) => name switch
    {
        "OnePoint" => new OnePointCross(),
        "TwoPoint" => new TwoPointCross(),
        "Uniform" => new UniformCross(0.5),
        "NPoint" => new NPointCross(3),
        "HUX" => new HUXCross(),
        "Arithmetic" => new ArithmeticCross(2),
        "Partial" => new PartialCross(),
        "SegmentSwap" => new SegmentSwapCross(),
        _ => new UniformCross(0.5)
    };

    private static IMutation<BinaryEvolutional> CreateMutation(string name) => name switch
    {
        "Random" => new RandomMutation(),
        "KFlip" => new KFlipMutation(2),
        "BlockFlip" => new BlockFlipMutation(),
        "MultiBlockFlip" => new MultiBlockFlipMutation(2),
        "RunFlip" => new RunFlipMutation(),
        "SwapPairs" => new SwapPairsMutation(2),
        "BitMask" => new BitMaskMutation(),
        "ShiftRotation" => new ShiftRotationMutation(),
        "NonUniform" => new NonUniformMutation(),
        "GeometricBlock" => new GeometricBlockMutation(),
        "Scramble" => new ScrambleMutation(),
        _ => new RandomMutation()
    };

    private static ISelection CreateSelection(string name, double fraction, int tournamentK) => name switch
    {
        "Tournament" => new TournamentSelection(tournamentK, fraction),
        "RouletteWheel" => new RouletteWheelSelection(fraction),
        "Rank" => new RankSelection(fraction),
        "Truncation" => new TruncationSelection(fraction),
        "SUS" => new StochasticUniversalSamplingSelection(fraction),
        "Elite" => new EliteSelecction(fraction),
        _ => new TournamentSelection(tournamentK, fraction)
    };

    private void SafeUiUpdate(Action uiAction)
    {
        if (IsDisposed || !IsHandleCreated)
            return;

        if (InvokeRequired)
            Invoke(uiAction);
        else
            uiAction();
    }

    private void ToggleRunState(bool isRunning)
    {
        _runButton.Enabled = !isRunning;
        _stopButton.Enabled = isRunning;
    }

    private void ClearOutputs()
    {
        _fitnessChart.ClearSeries();
        _populationChart.ClearSeries();

        _logList.Items.Clear();
    }

    private GaSettings ReadSettings() => new(
        Problem: _problemCombo.SelectedItem?.ToString() ?? "OneMax",
        SelectionType: _selectionCombo.SelectedItem?.ToString() ?? "Tournament",
        CrossType: _crossCombo.SelectedItem?.ToString() ?? "Uniform",
        MutationType: _mutationCombo.SelectedItem?.ToString() ?? "Random",
        PopulationSize: (int)_populationNum.Value,
        Generations: (int)_generationsNum.Value,
        GeneCount: (int)_geneCountNum.Value,
        Capacity: (int)_capacityNum.Value,
        MutationProbability: (double)_mutationProbNum.Value,
        EnableDiversity: _enableDiversityCheck.Checked,
        DiversityPenalty: (double)_diversityPenaltyNum.Value,
        SelectionFraction: (double)_selectionFractionNum.Value,
        TournamentK: (int)_tournamentKNum.Value,
        ParallelEval: _enableParallelEvalCheck.Checked,
        ParallelBreed: _enableParallelBreedCheck.Checked);

    private void UpdateProblemTooltip(string problem)
    {
        if (!ProblemDescriptions.TryGetValue(problem, out var description))
            description = "Undocumented problem.";

        _toolTip.SetToolTip(_problemCombo, description);
    }

    private static Panel CreateLabeled(string label, Control control)
    {
        var panel = new Panel { Width = Math.Max(160, control.Width + 12), Height = 52, Margin = new Padding(6, 2, 6, 2) };
        panel.Controls.Add(new Label { Text = label, Left = 0, Top = 0, Width = panel.Width - 4 });
        control.Left = 0;
        control.Top = 22;
        panel.Controls.Add(control);
        return panel;
    }

    private static ComboBox CreateCombo(int width) => new()
    {
        Width = width,
        DropDownStyle = ComboBoxStyle.DropDownList
    };

    private static NumericUpDown CreateNum(decimal min, decimal max, decimal value, int decimals) => new()
    {
        Width = 110,
        Minimum = min,
        Maximum = max,
        Value = value,
        DecimalPlaces = decimals,
        Increment = decimals == 0 ? 1 : 0.01M
    };

    private static SimpleLineChart CreateChart(string title, string[] seriesNames)
    {
        var chart = new SimpleLineChart { Dock = DockStyle.Fill, Title = title };
        var colors = new[]
        {
            Color.DodgerBlue,
            Color.DarkOrange,
            Color.ForestGreen,
            Color.Crimson,
            Color.MediumPurple,
            Color.DarkCyan
        };

        for (int i = 0; i < seriesNames.Length; i++)
            chart.AddSeries(seriesNames[i], colors[i % colors.Length]);

        return chart;
    }

    private sealed class SimpleLineChart : Panel
    {
        private readonly Dictionary<string, List<(double X, double Y)>> _series = new();
        private readonly Dictionary<string, Color> _colors = new();
        public string Title { get; set; } = string.Empty;

        public SimpleLineChart()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            BorderStyle = BorderStyle.FixedSingle;
        }

        public void AddSeries(string name, Color color)
        {
            if (_series.ContainsKey(name))
                return;

            _series[name] = [];
            _colors[name] = color;
            Invalidate();
        }

        public void AddPoint(string seriesName, double x, double y)
        {
            if (!_series.TryGetValue(seriesName, out var points))
                return;

            points.Add((x, y));
            Invalidate();
        }

        public void ClearSeries()
        {
            foreach (var points in _series.Values)
                points.Clear();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var bounds = ClientRectangle;
            if (bounds.Width < 60 || bounds.Height < 60)
                return;

            var plot = new RectangleF(bounds.Left + 50, bounds.Top + 25, bounds.Width - 70, bounds.Height - 60);
            using var axisPen = new Pen(Color.Gray, 1);
            e.Graphics.DrawRectangle(axisPen, plot.X, plot.Y, plot.Width, plot.Height);
            using var titleBrush = new SolidBrush(Color.Black);
            e.Graphics.DrawString(Title, Font, titleBrush, 8, 4);

            var allPoints = _series.Values.SelectMany(v => v).ToList();
            if (allPoints.Count == 0)
                return;

            double minX = allPoints.Min(p => p.X);
            double maxX = allPoints.Max(p => p.X);
            double minY = allPoints.Min(p => p.Y);
            double maxY = allPoints.Max(p => p.Y);

            if (Math.Abs(maxX - minX) < 1e-9) maxX = minX + 1;
            if (Math.Abs(maxY - minY) < 1e-9) maxY = minY + 1;

            foreach (var kv in _series)
            {
                var pts = kv.Value;
                if (pts.Count < 2)
                    continue;

                using var pen = new Pen(_colors[kv.Key], 2);
                for (int i = 1; i < pts.Count; i++)
                {
                    var a = ToScreen(pts[i - 1], minX, maxX, minY, maxY, plot);
                    var b = ToScreen(pts[i], minX, maxX, minY, maxY, plot);
                    e.Graphics.DrawLine(pen, a, b);
                }
            }

            int legendY = (int)plot.Y + 6;
            int legendX = (int)plot.Right - 145;
            foreach (var name in _series.Keys)
            {
                using var brush = new SolidBrush(_colors[name]);
                e.Graphics.FillRectangle(brush, legendX, legendY + 3, 10, 10);
                e.Graphics.DrawString(name, Font, Brushes.Black, legendX + 14, legendY);
                legendY += 16;
            }

            e.Graphics.DrawString(minY.ToString("F1", CultureInfo.InvariantCulture), Font, Brushes.DimGray, 6, plot.Bottom - 8);
            e.Graphics.DrawString(maxY.ToString("F1", CultureInfo.InvariantCulture), Font, Brushes.DimGray, 6, plot.Top - 8);
            e.Graphics.DrawString(minX.ToString("F0", CultureInfo.InvariantCulture), Font, Brushes.DimGray, plot.Left - 8, plot.Bottom + 2);
            e.Graphics.DrawString(maxX.ToString("F0", CultureInfo.InvariantCulture), Font, Brushes.DimGray, plot.Right - 20, plot.Bottom + 2);
        }

        private static PointF ToScreen((double X, double Y) p, double minX, double maxX, double minY, double maxY, RectangleF plot)
        {
            float x = (float)(plot.Left + (p.X - minX) * plot.Width / (maxX - minX));
            float y = (float)(plot.Bottom - (p.Y - minY) * plot.Height / (maxY - minY));
            return new PointF(x, y);
        }
    }

    private readonly record struct GaSettings(
        string Problem,
        string SelectionType,
        string CrossType,
        string MutationType,
        int PopulationSize,
        int Generations,
        int GeneCount,
        int Capacity,
        double MutationProbability,
        bool EnableDiversity,
        double DiversityPenalty,
        double SelectionFraction,
        int TournamentK,
        bool ParallelEval,
        bool ParallelBreed);
}
