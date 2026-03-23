# DarwinGA — Genetic Algorithm Library for .NET 8 (C#)

DarwinGA is a **lightweight, extensible genetic algorithm library for .NET 8** built in C# with clean abstractions for **selection, mutation, crossover, and termination**. It includes ready‑to‑use evolution models for **binary chromosomes** and **neural network optimization**, plus optional parallel execution for faster evaluations.

If you are looking for a **modern genetic algorithm framework in .NET**, DarwinGA offers a simple, pragmatic API with explicit operators and minimal ceremony.

## Why DarwinGA?

DarwinGA focuses on **clarity, extensibility, and performance**:

- **Generic core** with `GeneticAlgorithm<TElement>`.
- **Pluggable operators** (`ISelection`, `IMutation<T>`, `ICross<T>`, `ITermination`).
- **Create an evolution model by defining only crossover and mutation**, keeping the full power of a genetic algorithm with minimal setup.
- **Built‑in evolution models**:
  - `BinaryEvolutional` for classic bit‑string problems.
  - `ActivationNetworkEvolutional` for optimizing neural networks (`Accord.Neuro`).
- **Parallel evaluation and breeding** for faster runs.
- **.NET 8 + C# 12** ready.

## Quick Start (OneMax Example)

Below is a minimal example that maximizes the number of ones in a binary chromosome.

```csharp
using DarwinGA;

var ga = new GeneticAlgorithm<BitArray>(
    new Population<BitArray>(200, 10),
    new Selection<BitArray>(new RouletteWheelSelection()),
    new Crossover<BitArray>(new UniformCrossover()),
    new Mutation<BitArray>(new FlipBitMutation()),
    new Termination(100)
    );

ga.Start();
```

## How to Use

1. **Define your evolution model** (`BinaryEvolutional`, `ActivationNetworkEvolutional`, or your own).
2. **Assign operators**: selection, mutation, crossover, termination.
3. **Provide a fitness function**.
4. **Run the algorithm** with a population size.

### What is an Evolutional?

An **evolutional** is the chromosome type that implements `IGAEvolutional<T>`.  
To create a new one, you only need to define how it **crosses** (`ICross<T>`) and **mutates** (`IMutation<T>`). DarwinGA provides the rest of the genetic algorithm machinery (selection, termination, population flow).

**Minimal custom evolutional (crossover + mutation only):**

```csharp   
public class MyCustomChromosome : IGAEvolutional<MyCustomChromosome>
{
    // Your gene properties here

    public MyCustomChromosome Cross(MyCustomChromosome mate)
    {
        // Implement crossover logic
    }

    public void Mutate()
    {
        // Implement mutation logic
    }
}
```

### Parallel Execution

Enable performance‑optimized runs with:

```csharp
ga.EnableParallelism();
```

Or configure degree of parallelism:

```csharp
ga.SetDegreeOfParallelism(4);
```

Now, fitter genomes breed faster across multiple cores.

## Goals

DarwinGA targets:

- **Simplicity**: Easy to learn and use.
- **Flexibility**: Customize anything, from chromosome structure to fitness evaluation.
- **Performance**: Take advantage of modern hardware with parallel processing.

## Features

- [x] Binary, neural network, and custom chromosomes
- [x] Multiple selection, mutation, and crossover strategies
- [x] Parallel evaluation and genetic operations
- [x] Extensible architecture

## Installation

Get the latest release from [NuGet](https://www.nuget.org/packages/DarwinGA):

```
dotnet add package DarwinGA
```

## Examples

See the `Samples` directory for:

- **OneMax**: Maximize number of 1-bits.
- **Knapsack**: Maximize value in a weight constrained bag.
- **Messenger RNA Folding**: Evolve RNA sequences to folded structures.

## Documentation

API references and advanced topics are in the [wiki](https://github.com/username/repo/wiki).

## Contributing

Contributions are welcome! See `CONTRIBUTING.md` for details.

## License

DarwinGA is licensed under the MIT License. See `LICENSE` for details.

## How DarwinGA Differs From Similar Libraries

DarwinGA is designed to be **simple yet extensible**, avoiding the heavy configuration or overly abstract pipelines found in many GA frameworks.

**Key differences:**

- **Minimal boilerplate**: configure a GA in a few lines.
- **Define only crossover and mutation to build a full evolution model**, keeping the algorithm powerful yet lightweight to set up.
- **Explicit operators**: no hidden pipeline; you control selection, mutation, crossover, termination.
- **Binary & neural evolution built in**.
- **Parallelism built into the core**.
- **Modern .NET 8 API design**.

This makes DarwinGA a practical choice for **research, prototyping, and production‑ready optimization** in C#.

## Repository Structure

- `DarwinGA/` → Core library.
- `DarwinGA.Example/` → Console demo (OneMax).
- `DarwinGA.Tests/` → xUnit tests.

## Build

---

DarwinGA is developed with VS 2022, .NET 8, and C# 12. Last updated: $(date).

## Run Example

## Run Tests

## Dependencies

- `Accord.Neuro`
- `Accord.Statistics`

---

**Keywords for search**: genetic algorithm .NET, GA library C#, evolutionary algorithms .NET 8, genetic optimization C#, binary chromosome GA, neural network GA.

**Usage:**

