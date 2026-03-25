<div align="center">

# 🧬 DarwinGA

### A High-Performance Genetic Algorithm Engine for .NET

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://opensource.org/licenses/MIT)
[![NuGet](https://img.shields.io/nuget/v/DarwinGA)](https://www.nuget.org/packages/DarwinGA)

**DarwinGA** is a modular, extensible genetic algorithm framework for .NET 8.  
Define your problem. Let evolution find the solution.

[Quick Start](#-quick-start) · [Features](#-features-at-a-glance) · [Operators](#-built-in-operators) · [Examples](#-full-example) · [API Reference](#-api-reference)

</div>

---

## ❓ What Is DarwinGA?

**DarwinGA** is a genetic algorithm (GA) library that lets you solve optimization problems using the principles of natural evolution: **selection**, **crossover**, **mutation**, and **survival of the fittest**.

Instead of coding a solver, you describe:

1. **What a solution looks like** (chromosome)
2. **How to evaluate it** (fitness function)
3. **How it mutates and combines** (genetic operators)

The engine evolves a population of candidate solutions across generations until it converges on the best one.

---

## ✨ Features at a Glance

| Feature | DarwinGA | Other .NET GA libs |
|---|:---:|:---:|
| 🎯 Fully generic — evolve **any** type | ✅ | Partial |
| ⚡ Built-in **parallel evaluation & breeding** | ✅ | ❌ / External |
| 🧬 **11 mutation** operators (binary) | ✅ | 2–4 typical |
| 🔀 **8 crossover** operators (binary) | ✅ | 2–3 typical |
| 🏆 **7 selection** strategies | ✅ | 2–3 typical |
| 🌍 Built-in **diversity preservation** | ✅ | ❌ |
| 🧓 **Age-based selection** to prevent stagnation | ✅ | ❌ |
| 🧠 **Neuroevolution** support (ActivationNetwork) | ✅ | ❌ |
| 🔌 Extensible via interfaces | ✅ | Partial |
| 🪶 Lightweight — minimal dependencies | ✅ | Varies |
| 🏗️ Modern .NET 8 with `required` properties | ✅ | Often .NET Standard / Legacy |

---

## 📦 Installation

```bash
dotnet add package DarwinGA
```

Or via the NuGet Package Manager in Visual Studio:

```
Install-Package DarwinGA
```

---

## 🚀 Quick Start

Three steps to evolve a solution:

### 1️⃣ Define Your Chromosome

Implement `IGAEvolutional<T>` on any class that represents a candidate solution:

```csharp
using DarwinGA.Interfaces;

public class PriceSolution : IGAEvolutional<PriceSolution>
{
    public double Price { get; set; }
}
```

### 2️⃣ Implement Mutation and Crossover

```csharp
public class PriceMutation : IMutation<PriceSolution>
{
    public void Apply(PriceSolution evo, double mutationProb)
    {
        if (MyRandom.NextDouble() < mutationProb)
            evo.Price += MyRandom.NextDouble() * 2 - 1;
    }
}

public class PriceCrossover : ICross<PriceSolution>
{
    public PriceSolution Apply(PriceSolution a, PriceSolution b)
    {
        return new PriceSolution
        {
            Price = (a.Price + b.Price) / 2.0
        };
    }
}
```

### 3️⃣ Configure and Run

```csharp
using DarwinGA;
using DarwinGA.Selections;
using DarwinGA.Terminations;

var ga = new GeneticAlgorithm<PriceSolution>
{
    NewItem = () => new PriceSolution { Price = MyRandom.NextDouble() * 100 },

    Fitness = solution =>
    {
        double demand = 100 - solution.Price;
        return solution.Price * demand; // maximize profit
    },

    Mutation            = new PriceMutation(),
    Cross               = new PriceCrossover(),
    MutationProbability = 0.05,

    Selection   = new TournamentSelection(tournamentSize: 5),
    Termination = new GenerationNumTermination(maxGenerations: 300),

    OnNewGeneration = result =>
    {
        Console.WriteLine($"Gen {result.GenerationNum}: Best Fitness = {result.BestFitness:F2}");
    }
};

ga.Run(populationSize: 100);
```

That's it. The algorithm handles the rest.

---

## 🧰 Built-in Operators

### 🏆 Selection Strategies (7)

Choose how parents are picked for breeding:

| Strategy | Class | Description |
|---|---|---|
| **Tournament** | `TournamentSelection` | Picks the best of *k* random contenders. Good general-purpose default. |
| **Elite** | `EliteSelecction` | Takes the top fraction by fitness deterministically. |
| **Roulette Wheel** | `RouletteWheelSelection` | Probability proportional to fitness value. |
| **Rank** | `RankSelection` | Probability proportional to rank, not raw fitness. Avoids super-fit dominance. |
| **Truncation** | `TruncationSelection` | Deterministically takes the top *N%*. Simple and fast. |
| **Stochastic Universal Sampling** | `StochasticUniversalSamplingSelection` | Evenly-spaced pointers over the fitness distribution. Less bias than roulette. |
| **Age-Based** *(decorator)* | `AgeBasedSelection` | Wraps any selection and penalizes individuals that survive too many generations. |

```csharp
// Tournament selection keeping top 50% via 5-way tournaments
Selection = new TournamentSelection(tournamentSize: 5, selectionFraction: 0.5)

// Age-based on top of tournament (5% penalty per generation)
EnableAgeBasedSelection = true,
AgePenaltyFactor = 0.05,
Selection = new TournamentSelection(5)
```

---

### 🧬 Mutation Operators — Binary (11)

Control how chromosomes are randomly altered:

| Mutation | Class | Behavior |
|---|---|---|
| **Random** | `RandomMutation` | Flips each bit independently with given probability. |
| **K-Flip** | `KFlipMutation` | Flips exactly *k* random bits. |
| **Block Flip** | `BlockFlipMutation` | Flips a contiguous block of bits. |
| **Multi-Block Flip** | `MultiBlockFlipMutation` | Flips multiple random contiguous blocks. |
| **Bit Mask** | `BitMaskMutation` | Applies a random bitmask to flip bits. |
| **Geometric Block** | `GeometricBlockMutation` | Block size sampled from a geometric distribution. |
| **Non-Uniform** | `NonUniformMutation` | Mutation intensity decreases over time. |
| **Run Flip** | `RunFlipMutation` | Flips a run of consecutive identical bits. |
| **Scramble** | `ScrambleMutation` | Shuffles the order of bits in a random segment. |
| **Shift Rotation** | `ShiftRotationMutation` | Rotates (shifts) a segment of the chromosome. |
| **Swap Pairs** | `SwapPairsMutation` | Swaps values of randomly chosen pairs of genes. |

---

### 🔀 Crossover Operators — Binary (8)

Define how two parents produce offspring:

| Crossover | Class | Behavior |
|---|---|---|
| **One-Point** | `OnePointCross` | Single cut point; left from parent A, right from parent B. |
| **Two-Point** | `TwoPointCross` | Two cut points; middle segment swapped. |
| **N-Point** | `NPointCross` | Generalized *n* cut points. |
| **Uniform** | `UniformCross` | Each gene chosen from either parent with a given probability. |
| **Arithmetic** | `ArithmeticCross` | Weighted average of parent gene values. |
| **HUX (Half-Uniform)** | `HUXCross` | Swaps exactly half of the differing bits. |
| **Partial** | `PartialCross` | Crosses a partial random segment. |
| **Segment Swap** | `SegmentSwapCross` | Swaps a random contiguous segment between parents. |

---

### 🛑 Termination Conditions (2)

Stop evolution when a condition is met:

| Condition | Class | Description |
|---|---|---|
| **Generation Limit** | `GenerationNumTermination` | Stops after *N* generations. |
| **Fitness Threshold** | `FitnessThresholdTermination` | Stops when fitness reaches a target value. |

```csharp
// Stop after 500 generations
Termination = new GenerationNumTermination(500)

// Stop when fitness >= 0.98
Termination = new FitnessThresholdTermination(0.98)
```

---

### 🌍 Diversity Preservation

Prevent premature convergence by penalizing similar individuals:

```csharp
EnableDiversity = true,

// Define how to measure distance between two individuals
DiversityMetric = new DelegateDiversityMetric<MyChromosome>(
    (a, b) => /* return a double representing distance */
),

// Penalize fitness of similar individuals (higher factor = stronger penalty)
DiversityStrategy = new SimilarityPenaltyStrategy<MyChromosome>(penaltyFactor: 0.5)
```

You can also implement `IDiversityMetric<T>` and `IDiversityStrategy<T>` for custom behavior.

---

### ⚡ Parallel Processing

Speed up evolution on multi-core machines:

```csharp
// Evaluate fitness in parallel across the population
EnableParallelEvaluation = true,

// Breed children in parallel
EnableParallelBreeding = true,

// Optionally configure parallelism limits
ParallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 8 }
```

---

## 🧠 Neuroevolution Support

DarwinGA includes built-in support for evolving neural networks via `ActivationNetworkEvolutional`:

```csharp
using DarwinGA.Evolutionals.ActivationNetworkEvolutional;

var ga = new GeneticAlgorithm<ActivationNetworkEvolutional>
{
    NewItem = () => new ActivationNetworkEvolutional(
        neuronsCount: new[] { 5, 3, 1 },
        inputsCount: 4
    ),

    Fitness = individual =>
    {
        // Evaluate the neural network on your dataset
        double[] output = individual.NeuralNetwork.Compute(inputData);
        return CalculateAccuracy(output, expected);
    },

    Mutation    = new ActivationNetworkMutation(),
    Cross       = new ActivationNetworkCrossover(),
    Selection   = new TournamentSelection(5),
    Termination = new GenerationNumTermination(1000),
    OnNewGeneration = r =>
        Console.WriteLine($"Gen {r.GenerationNum}: {r.BestFitness:F4}")
};

ga.Run(populationSize: 50);
```

Evolve neural network topologies and weights without backpropagation — ideal for reinforcement learning, game AI, and control systems.

---

## 📋 Full Example

The **OneMax** problem: maximize the number of `1` bits in a binary chromosome.

```csharp
using DarwinGA;
using DarwinGA.Diversity;
using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Evolutionals.BinaryEvolutional.Crossers;
using DarwinGA.Evolutionals.BinaryEvolutional.Mutations;
using DarwinGA.Selections;
using DarwinGA.Terminations;

int chromosomeLength = 200;

var ga = new GeneticAlgorithm<BinaryEvolutional>()
{
    NewItem = () =>
    {
        var chr = new BinaryEvolutional(chromosomeLength);
        for (int i = 0; i < chromosomeLength; i++)
            chr.SetGen(i, MyRandom.NextDouble() < 0.5);
        return chr;
    },

    Fitness = individual =>
    {
        int ones = 0;
        for (int i = 0; i < individual.Size; i++)
            if (individual.GetGen(i)) ones++;
        return (double)ones / individual.Size;
    },

    MutationProbability    = 0.10,
    Mutation               = new KFlipMutation(2),
    Cross                  = new UniformCross(0.5),
    Selection              = new TournamentSelection(8),
    Termination            = new FitnessThresholdTermination(0.98),

    EnableParallelEvaluation = true,

    EnableDiversity  = true,
    DiversityMetric  = new DelegateDiversityMetric<BinaryEvolutional>((a, b) =>
    {
        int diff = 0;
        for (int i = 0; i < a.Size; i++)
            if (a.GetGen(i) != b.GetGen(i)) diff++;
        return diff;
    }),
    DiversityStrategy = new SimilarityPenaltyStrategy<BinaryEvolutional>(penaltyFactor: 0.6),

    OnNewGeneration = result =>
    {
        Console.WriteLine(
            $"Gen {result.GenerationNum,-4} | Fitness: {result.BestFitness:F4}");
    }
};

ga.Run(populationSize: 120);
```

---

## 🔌 API Reference

### `GeneticAlgorithm<T>` — Main Engine

| Property | Type | Description |
|---|---|---|
| `NewItem` | `Func<T>` | Factory that creates a new random individual. **Required.** |
| `Fitness` | `Func<T, double>` | Evaluates how good a solution is. Higher = better. **Required.** |
| `Mutation` | `IMutation<T>` | Mutation operator. **Required.** |
| `Cross` | `ICross<T>` | Crossover operator. **Required.** |
| `Selection` | `ISelection` | Parent selection strategy. **Required.** |
| `Termination` | `ITermination` | When to stop evolving. **Required.** |
| `OnNewGeneration` | `Action<GenerationResult<T>>` | Callback invoked after each generation. **Required.** |
| `MutationProbability` | `double` | Probability of mutating each child. Default: `0.01`. |
| `EnableParallelEvaluation` | `bool` | Evaluate fitness in parallel. Default: `false`. |
| `EnableParallelBreeding` | `bool` | Breed children in parallel. Default: `false`. |
| `ParallelOptions` | `ParallelOptions` | Configure parallelism (e.g. max threads). |
| `EnableDiversity` | `bool` | Enable diversity preservation. Default: `false`. |
| `DiversityMetric` | `IDiversityMetric<T>?` | Distance function between individuals. |
| `DiversityStrategy` | `IDiversityStrategy<T>?` | Strategy to adjust fitness based on diversity. |
| `EnableAgeBasedSelection` | `bool` | Penalize long-lived individuals. Default: `false`. |
| `AgePenaltyFactor` | `double` | Fitness penalty per generation of age. Default: `0.05`. |

### Key Interfaces

| Interface | Purpose |
|---|---|
| `IGAEvolutional<T>` | Marker for any evolvable type (your chromosome). |
| `IMutation<T>` | `void Apply(T evo, double mutationProb)` — mutate an individual. |
| `ICross<T>` | `T Apply(T a, T b)` — produce a child from two parents. |
| `ISelection` | `IEnumerable<FitnessResult> Select(...)` — choose parents from the population. |
| `ITermination` | `bool ShouldTerminate(GenerationResultBase result)` — stop condition. |
| `IDiversityMetric<T>` | `double Distance(T a, T b)` — measure distance between individuals. |
| `IDiversityStrategy<T>` | Adjust fitness scores to reward diversity. |

---

## 🧠 How to Think in Evolutionary Terms

Instead of asking:

> ❌ *"How do I solve this problem?"*

Ask:

> ✅ *"What does a solution look like?"* → Define your chromosome  
> ✅ *"How do I measure if it's good?"* → Write a fitness function  
> ✅ *"How can I slightly modify it?"* → Choose mutation & crossover operators  

The algorithm handles the rest.

---

## 💡 Tips for Good Results

| ✅ Do | ❌ Avoid |
|---|---|
| Design a **meaningful fitness function** | Fitness too flat → no evolutionary pressure |
| Use a representation that can **evolve smoothly** | Mutation too aggressive → chaotic search |
| Enable **diversity** for complex landscapes | No diversity → premature convergence |
| Tune **mutation probability** (start ~0.01–0.10) | Overfitting to a single test case |
| Try **different selection strategies** | Using only elitism (loses diversity) |

---

## 🔥 Use Case Ideas

DarwinGA can optimize any problem you can encode as a chromosome:

| Domain | Example |
|---|---|
| 💰 **Pricing** | Find the price that maximizes profit |
| 📦 **Logistics** | Optimize warehouse placement or bin packing |
| 🚚 **Routing** | Evolve delivery routes (TSP variants) |
| 🧠 **Neuroevolution** | Evolve neural network weights for game AI |
| 🎯 **Strategy** | Optimize game strategies or decision trees |
| 📊 **Feature Selection** | Select the best subset of features for ML models |
| 🏗️ **Scheduling** | Job-shop scheduling, resource allocation |
| 🔧 **Parameter Tuning** | Hyperparameter optimization for any system |

---

## 🤝 Contributing

Contributions are welcome! Feel free to open issues or submit pull requests.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## 📄 License

This project is licensed under the [MIT License](https://opensource.org/licenses/MIT).

---

<div align="center">

**Made with 🧬 by [Gerardo Tous](https://github.com/G3r4rd00)**

*If DarwinGA helped your project, consider giving it a ⭐ on GitHub!*

</div>
