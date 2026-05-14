using DarwinGA.AI;
using DarwinGA.Example;
using Microsoft.Extensions.Configuration;

// Add your logic to use openAiService here.
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
            var aiProvider = GetAIProvider();
            Example05_AICrosser.Run(aiProvider);
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
            var aiProvider = GetAIProvider();
            Example06_JobShopScheduling.RunWithAICrosser(aiProvider);
            break;
        default:
            Console.WriteLine("Invalid option.");
            break;
    }
}

static IAIProvider GetAIProvider()
{
    const string defaultSettingsFile = "appsettings.json";
    const string debugSettingsFile = "appsettings.debug.json";
    const string legacyDebugSettingsFile = "appsettings.Debug.json";

    var basePath = Directory.GetCurrentDirectory();
    var settingsFile = File.Exists(Path.Combine(basePath, debugSettingsFile))
        ? debugSettingsFile
        : File.Exists(Path.Combine(basePath, legacyDebugSettingsFile))
            ? legacyDebugSettingsFile
            : defaultSettingsFile;

    var configuration = new ConfigurationBuilder()
        .SetBasePath(basePath)
        .AddJsonFile(settingsFile, optional: false, reloadOnChange: true)
        .Build();

    Console.WriteLine("Choose an AI provider:");
    Console.WriteLine(" 1) OpenAI");
    Console.WriteLine(" 2) DeepSeek");
    Console.Write("Option (1-2): ");
    var providerOption = Console.ReadLine()?.Trim();

    if (providerOption == "2")
        return CreateDeepSeekProvider(configuration);

    return CreateOpenAIProvider(configuration);
}

static IAIProvider CreateOpenAIProvider(IConfiguration configuration)
{
    string? apiKey = configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");

    if (string.IsNullOrWhiteSpace(apiKey))
        throw new InvalidOperationException("OpenAI API key not found in appsettings.json or environment variables.");

    // Load available models if the provider supports it
    Console.WriteLine("Choose a model:");
    Console.WriteLine(" 1) gpt-5.5");
    Console.WriteLine(" 2) gpt-5.5-pro");
    Console.WriteLine(" 3) gpt-5.4");
    Console.WriteLine(" 4) gpt-4o");
    Console.WriteLine(" 5) gpt-4");
    Console.WriteLine(" 6) gpt-3.5-turbo");
    Console.Write("Option (1-6): ");
    var option = Console.ReadLine()?.Trim();

    string model = option switch
    {
        "1" => "gpt-5.5",
        "2" => "gpt-5.5-pro",
        "3" => "gpt-5.4",
        "4" => "gpt-4o",
        "5" => "gpt-4",
        "6" => "gpt-3.5-turbo",
        _ => "gpt-3.5-turbo"
    };

    return new ChatGPTProvider(apiKey, model);
}

static IAIProvider CreateDeepSeekProvider(IConfiguration configuration)
{
    string? apiKey = configuration["DeepSeek:ApiKey"] ?? Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");

    if (string.IsNullOrWhiteSpace(apiKey))
        throw new InvalidOperationException("DeepSeek API key not found in appsettings.json or environment variables.");

    Console.WriteLine("Choose a model:");
    Console.WriteLine(" 1) deepseek-chat");
    Console.WriteLine(" 2) deepseek-reasoner");
    Console.Write("Option (1-2): ");
    var option = Console.ReadLine()?.Trim();

    string model = option switch
    {
        "2" => "deepseek-reasoner",
        _ => "deepseek-chat"
    };

    string baseUrl = configuration["DeepSeek:BaseUrl"] ?? "https://api.deepseek.com";

    return new DeepSeekProvider(apiKey, model, baseUrl: baseUrl);
}
