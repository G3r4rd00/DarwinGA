using DarwinGA.Example;

while (true)
{
    Console.Clear();

    Console.WriteLine("DarwinGA - Examples");
    Console.WriteLine("===================\n");

    Console.WriteLine("Select problem type:");
    Console.WriteLine("  1) 0/1 Knapsack");
    Console.WriteLine("  2) Job Shop Scheduling");
    Console.WriteLine("  3) Neural network evolution (XOR)\n");
    Console.Write("Option (1-3): ");
    var problemOption = Console.ReadLine()?.Trim();

    switch (problemOption)
    {
        case "1":
            RunKnapsackMenu();
            break;
        case "2":
            RunJobShopMenu();
            break;
        case "3":
            Example04_NeuralNetworkXor.Run();
            break;
        default:
            Console.WriteLine("Invalid option.");
            break;
    }

    Console.WriteLine("\nPress any key to return to the menu...");
    Console.ReadKey();
}

static void RunKnapsackMenu()
{
    Console.Clear();
    Console.WriteLine("DarwinGA - 0/1 Knapsack");
    Console.WriteLine("========================\n");

    Console.WriteLine("Choose an algorithm:");
    Console.WriteLine("  1) + Diversity + Generation statistics");
    Console.WriteLine("  2) + Island Model (ring migration)");
    Console.WriteLine("  3) + AI Population Crosser\n");

    Console.Write("Option (1-3): ");
    var option = Console.ReadLine()?.Trim();

    switch (option)
    {
        case "1":
            Example01_OneMaxWithStatistics.Run();
            break;
        case "2":
            Example03_IslandModel.Run();
            break;
        case "3":
            Example05_AICrosser.Run();
            break;
        default:
            Console.WriteLine("Invalid option.");
            break;
    }
}

static void RunJobShopMenu()
{
    Console.Clear();
    Console.WriteLine("DarwinGA - Job Shop Scheduling");
    Console.WriteLine("==============================\n");

    Console.WriteLine("Choose an algorithm:");
    Console.WriteLine("  1) + Diversity + Generation statistics");
    Console.WriteLine("  2) + Island Model (ring migration)");
    Console.WriteLine("  3) + AI Population Crosser\n");

    Console.Write("Option (1-3): ");
    var option = Console.ReadLine()?.Trim();

    switch (option)
    {
        case "1":
            Example06_JobShopScheduling.RunWithDiversity();
            break;
        case "2":
            Example06_JobShopScheduling.RunWithIslandModel();
            break;
        case "3":
            Example06_JobShopScheduling.RunWithAICrosser();
            break;
        default:
            Console.WriteLine("Invalid option.");
            break;
    }
}
