## 🧩 Creating a Custom Evolutionary Problem

DarwinGA is designed to be **problem-agnostic**.

That means you don’t solve a problem directly — you **define how a solution behaves**, and the algorithm evolves it.

To use DarwinGA, you only need to define 3 things:

---

### 1️⃣ Define your Solution (Chromosome)

This represents a **candidate solution**.

```csharp
public class MySolution
{
    public double Price { get; set; }
}
```

👉 This is what evolves.

You can model anything:

* numbers
* arrays
* objects
* strategies

---

### 2️⃣ Define the Fitness Function

This is the **most important part**.

It tells the algorithm:

> “How good is this solution?”

```csharp
public class MyFitnessEvaluator : IFitnessEvaluator<MySolution>
{
    public double Evaluate(MySolution solution)
    {
        // Example: maximize profit
        double demand = 100 - solution.Price;
        double profit = solution.Price * demand;

        return profit;
    }
}
```

👉 The algorithm will try to **maximize this value**.

---

### 3️⃣ (Optional) Customize Mutation & Crossover

You can control how solutions evolve.

#### Mutation

```csharp
public class MyMutation : IMutationStrategy<MySolution>
{
    public void Mutate(MySolution solution)
    {
        solution.Price += Random.Shared.NextDouble() * 2 - 1;
    }
}
```

#### Crossover

```csharp
public class MyCrossover : ICrossoverStrategy<MySolution>
{
    public MySolution Crossover(MySolution a, MySolution b)
    {
        return new MySolution
        {
            Price = (a.Price + b.Price) / 2
        };
    }
}
```

👉 These define how evolution behaves.

---

## 🚀 Running the Algorithm

Once defined, you plug everything into the engine:

```csharp
var ga = new GeneticAlgorithm<MySolution>(
    populationSize: 100,
    mutationRate: 0.05,
    crossoverRate: 0.7,
    fitnessEvaluator: new MyFitnessEvaluator(),
    mutationStrategy: new MyMutation(),
    crossoverStrategy: new MyCrossover()
);

ga.Initialize(() => new MySolution
{
    Price = Random.Shared.NextDouble() * 100
});

ga.Run(generations: 200);

var best = ga.GetBestSolution();

Console.WriteLine($"Best price found: {best.Price}");
```

---

## 🧠 How to Think in Evolutionary Terms

Instead of asking:

❌ “How do I solve this problem?”

You ask:

✅ “What does a solution look like?”
✅ “How do I measure if it's good?”
✅ “How can I slightly modify it?”

That’s it.

The algorithm does the rest.

---

## 💡 Key Insight

A good evolutionary setup depends on:

* ✔️ A meaningful fitness function
* ✔️ A representation that can evolve smoothly
* ✔️ Balanced mutation (not too random, not too rigid)

---

## ⚠️ Common Mistakes

* ❌ Fitness function too simple → no evolution
* ❌ Mutation too aggressive → chaos
* ❌ No diversity → early stagnation
* ❌ Overfitting to a specific case

---

## 🔥 Real Example Ideas

You can build custom evolutions for:

* 💰 Pricing optimization
* 📦 Warehouse distribution
* 🚚 Delivery routes
* 🧠 AI parameter tuning
* 🎯 Strategy optimization

---

## 🧬 Final Mental Model

Think of DarwinGA as:

> A system where you define the **rules of survival**,
> and solutions fight to become the best.

---
