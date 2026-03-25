using DarwinGA.Example;

Console.WriteLine("DarwinGA - Examples");
Console.WriteLine("===================\n");

Console.WriteLine("Choose an example:");
Console.WriteLine("  1) 0/1 Knapsack + Diversity + Generation statistics");
Console.WriteLine("  2) 0/1 Knapsack + CancellationToken");
Console.WriteLine("  3) 0/1 Knapsack + Island Model (ring migration)");
Console.WriteLine("  4) Neural network evolution (XOR)\n");

Console.Write("Option (1-3): ");
var option = Console.ReadLine()?.Trim();

switch (option)
{
    case "1":
        Example01_OneMaxWithStatistics.Run();
        break;
    case "2":
        Example02_CancellationToken.Run();
        break;
    case "3":
        Example03_IslandModel.Run();
        break;
    case "4":
        Example04_NeuralNetworkXor.Run();
        break;
    default:
        Console.WriteLine("Invalid option.");
        break;
}
