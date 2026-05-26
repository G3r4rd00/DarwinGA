using DarwinGA.AI;
using DarwinGA.Example;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

var appConfiguration = LoadAppConfiguration();
var gaSettings = GeneticAlgorithmSettings.FromConfiguration(appConfiguration);

// Add your logic to use openAiService here.
while (true)
{
    Console.Clear();

    Console.WriteLine("DarwinGA - Examples");
    Console.WriteLine("===================\n");

    Console.WriteLine("Select problem type:");
    Console.WriteLine("  1) 0/1 Knapsack");
    Console.WriteLine("  2) Job Shop Scheduling");
    Console.WriteLine("  3) Neural network evolution (XOR)");
    Console.WriteLine("  4) Grid walker pathfinding (byte-chain chromosome)");
    Console.WriteLine("  5) Traveling Salesman (city distance matrix)\n");
    Console.Write("Option (1-5, default 1): ");
    var problemOption = Console.ReadLine()?.Trim();
    if (string.IsNullOrWhiteSpace(problemOption))
        problemOption = "1";

    switch (problemOption)
    {
        case "1":
            RunKnapsackMenu(gaSettings, appConfiguration);
            break;
        case "2":
            RunJobShopMenu(gaSettings, appConfiguration);
            break;
        case "3":
            Example04_NeuralNetworkXor.Run(gaSettings);
            break;
        case "4":
            RunGridWalkerMenu(gaSettings, appConfiguration);
            break;
        case "5":
            RunTravelingSalesmanMenu(gaSettings, appConfiguration);
            break;
        default:
            Console.WriteLine("Invalid option.");
            break;
    }

static void RunGridWalkerMenu(GeneticAlgorithmSettings gaSettings, IConfiguration appConfiguration)
{
    Console.Clear();
    Console.WriteLine("DarwinGA - Grid walker pathfinding");
    Console.WriteLine("================================\n");

    Console.WriteLine("Choose an algorithm:");
    Console.WriteLine("  1) + Diversity + Generation statistics");
    Console.WriteLine("  2) + Island Model (ring migration)");
    Console.WriteLine("  3) + AI Population Crosser\n");

    Console.Write("Option (1-3, default 3): ");
    var option = Console.ReadLine()?.Trim();
    if (string.IsNullOrWhiteSpace(option))
        option = "3";

    switch (option)
    {
        case "1":
            Example07_GridWalker.RunWithDiversity(gaSettings);
            break;
        case "2":
            Example07_GridWalker.RunWithIslandModel(gaSettings);
            break;
        case "3":
            var aiProvider = GetAIProvider(appConfiguration);
            Example07_GridWalker.RunWithAICrosser(aiProvider, gaSettings);
            break;
        default:
            Console.WriteLine("Invalid option.");
            break;
    }
}

static void RunTravelingSalesmanMenu(GeneticAlgorithmSettings gaSettings, IConfiguration appConfiguration)
{
    Console.Clear();
    Console.WriteLine("DarwinGA - Traveling Salesman");
    Console.WriteLine("==============================\n");

    Console.WriteLine("Choose an algorithm:");
    Console.WriteLine("  1) + Diversity + Generation statistics");
    Console.WriteLine("  2) + Island Model (ring migration)");
    Console.WriteLine("  3) + AI Population Crosser\n");

    Console.Write("Option (1-3, default 3): ");
    var option = Console.ReadLine()?.Trim();
    if (string.IsNullOrWhiteSpace(option))
        option = "3";

    switch (option)
    {
        case "1":
            Example08_TravelingSalesman.RunWithDiversity(gaSettings);
            break;
        case "2":
            Example08_TravelingSalesman.RunWithIslandModel(gaSettings);
            break;
        case "3":
            var aiProvider = GetAIProvider(appConfiguration);
            Example08_TravelingSalesman.RunWithAICrosser(aiProvider, gaSettings);
            break;
        default:
            Console.WriteLine("Invalid option.");
            break;
    }
}

    Console.WriteLine("\nPress any key to return to the menu...");
    Console.ReadKey();
}

static void RunKnapsackMenu(GeneticAlgorithmSettings gaSettings, IConfiguration appConfiguration)
{
    Console.Clear();
    Console.WriteLine("DarwinGA - 0/1 Knapsack");
    Console.WriteLine("========================\n");

    Console.WriteLine("Choose an algorithm:");
    Console.WriteLine("  1) + Diversity + Generation statistics");
    Console.WriteLine("  2) + Island Model (ring migration)");
    Console.WriteLine("  3) + AI Population Crosser\n");

    Console.Write("Option (1-3, default 3): ");
    var option = Console.ReadLine()?.Trim();
    if (string.IsNullOrWhiteSpace(option))
        option = "3";

    switch (option)
    {
        case "1":
            Example01_OneMaxWithStatistics.Run(gaSettings);
            break;
        case "2":
            Example03_IslandModel.Run(gaSettings);
            break;
        case "3":
            var aiProvider = GetAIProvider(appConfiguration);
            Example05_AICrosser.Run(aiProvider, gaSettings);
            break;
        default:
            Console.WriteLine("Invalid option.");
            break;
    }
}

static void RunJobShopMenu(GeneticAlgorithmSettings gaSettings, IConfiguration appConfiguration)
{
    Console.Clear();
    Console.WriteLine("DarwinGA - Job Shop Scheduling");
    Console.WriteLine("==============================\n");

    Console.WriteLine("Choose an algorithm:");
    Console.WriteLine("  1) + Diversity + Generation statistics");
    Console.WriteLine("  2) + Island Model (ring migration)");
    Console.WriteLine("  3) + AI Population Crosser\n");

    Console.Write("Option (1-3, default 3): ");
    var option = Console.ReadLine()?.Trim();
    if (string.IsNullOrWhiteSpace(option))
        option = "3";

    switch (option)
    {
        case "1":
            Example06_JobShopScheduling.RunWithDiversity(gaSettings);
            break;
        case "2":
            Example06_JobShopScheduling.RunWithIslandModel(gaSettings);
            break;
        case "3":
            var aiProvider = GetAIProvider(appConfiguration);
            Example06_JobShopScheduling.RunWithAICrosser(aiProvider, gaSettings);
            break;
        default:
            Console.WriteLine("Invalid option.");
            break;
    }
}

static IConfigurationRoot LoadAppConfiguration()
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

    return new ConfigurationBuilder()
        .SetBasePath(basePath)
        .AddJsonFile(settingsFile, optional: false, reloadOnChange: true)
        .Build();
}

static IAIProvider GetAIProvider(IConfiguration configuration)
{
    Console.WriteLine("Choose an AI provider:");
    Console.WriteLine(" 1) OpenAI");
    Console.WriteLine(" 2) DeepSeek");
    Console.WriteLine(" 3) LM Studio (OpenAI-compatible local API)");
    Console.Write("Option (1-3, default 3): ");
    var providerOption = Console.ReadLine()?.Trim();
    if (string.IsNullOrWhiteSpace(providerOption))
        providerOption = "3";

    if (providerOption == "3")
        return CreateLmStudioProvider(configuration);

    if (providerOption == "2")
        return CreateDeepSeekProvider(configuration);

    return CreateOpenAIProvider(configuration);
}

static IAIProvider CreateOpenAIProvider(IConfiguration configuration)
{
    string? apiKey = configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    int maxAccumulatedMessages = GetMaxAccumulatedMessages(configuration);

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

    return new ChatGPTProvider(apiKey, model, maxAccumulatedMessages: maxAccumulatedMessages);
}

static IAIProvider CreateDeepSeekProvider(IConfiguration configuration)
{
    string? apiKey = configuration["DeepSeek:ApiKey"] ?? Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
    int maxAccumulatedMessages = GetMaxAccumulatedMessages(configuration);

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

    return new DeepSeekProvider(apiKey, model, baseUrl: baseUrl, maxAccumulatedMessages: maxAccumulatedMessages);
}

static IAIProvider CreateLmStudioProvider(IConfiguration configuration)
{
    string baseUrl = configuration["LMStudio:BaseUrl"] ?? "http://localhost:1234/v1";
    string? apiKey = configuration["LMStudio:ApiKey"] ?? "sk-lm-mrpJFc9W:tpjJ8TAMJGZG1wYKJBkJ";
    int maxAccumulatedMessages = GetMaxAccumulatedMessages(configuration);

    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new InvalidOperationException("LM Studio BaseUrl was not found in appsettings.json.");

    var models = GetLmStudioModels(baseUrl, apiKey);
    string model = SelectLmStudioModel(models);

    if (string.IsNullOrWhiteSpace(model))
    {
        Console.Write("Model not found automatically. Enter LM Studio model id manually: ");
        model = Console.ReadLine()?.Trim() ?? string.Empty;
    }

    if (string.IsNullOrWhiteSpace(model))
        throw new InvalidOperationException("LM Studio model cannot be empty.");

    return new LMStudioProvider(baseUrl, model, apiKey, maxAccumulatedMessages: maxAccumulatedMessages);
}

static int GetMaxAccumulatedMessages(IConfiguration configuration)
{
    var rawValue = configuration["AI:MaxAccumulatedMessages"];
    return int.TryParse(rawValue, out var value) && value > 0 ? value : 3;
}

static List<string> GetLmStudioModels(string baseUrl, string? apiKey)
{
    try
    {
        using var httpClient = new HttpClient();
        if (!string.IsNullOrWhiteSpace(apiKey))
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");

        var normalizedBaseUrl = baseUrl.TrimEnd('/');
        var endpoint = $"{normalizedBaseUrl}/models";
        var response = httpClient.GetAsync(endpoint).GetAwaiter().GetResult();

        if (!response.IsSuccessStatusCode)
            return new List<string>();

        var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        using var document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty("data", out var dataElement) || dataElement.ValueKind != JsonValueKind.Array)
            return new List<string>();

        return dataElement.EnumerateArray()
            .Select(x => x.TryGetProperty("id", out var idElement) ? idElement.GetString() : null)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
    catch
    {
        return new List<string>();
    }
}

static string SelectLmStudioModel(IReadOnlyList<string> models)
{
    if (models.Count == 0)
        return string.Empty;

    Console.WriteLine("Choose a LM Studio model:");
    for (int i = 0; i < models.Count; i++)
        Console.WriteLine($" {i + 1}) {models[i]}");

    Console.Write($"Option (1-{models.Count}, default 1): ");
    var option = Console.ReadLine()?.Trim();

    if (int.TryParse(option, out var index) && index >= 1 && index <= models.Count)
        return models[index - 1];

    return models[0];
}
