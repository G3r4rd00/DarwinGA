using System.Text.Json;
using DarwinGA.AI;
using DarwinGA.Example;
using Microsoft.Extensions.Configuration;

var appConfiguration = LoadAppConfiguration();
var gaSettings = GeneticAlgorithmSettings.FromConfiguration(appConfiguration);

RunGridWalkerMenu(gaSettings, appConfiguration);
return;


static IAIProvider CreateDeepSeekProviderWithModel(IConfiguration configuration, string model)
{
    string? apiKey = configuration["DeepSeek:ApiKey"] ?? Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
    

    if (string.IsNullOrWhiteSpace(apiKey))
        throw new InvalidOperationException("DeepSeek API key not found in appsettings.json or environment variables.");

    string baseUrl = configuration["DeepSeek:BaseUrl"] ?? "https://api.deepseek.com";

    return new DeepSeekProvider(apiKey, model, baseUrl: baseUrl);
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




static void RunGridWalkerMenu(GeneticAlgorithmSettings gaSettings, IConfiguration appConfiguration)
{
    Console.Clear();
    Console.WriteLine("DarwinGA - Grid walker pathfinding");
    Console.WriteLine("================================\n");

    int cityCount = AskCityCount(appConfiguration);
    bool useAIPopulation = AskYesNo("Use AI for initial population creation? (Y/n): ", true);
    bool useAICrossover = AskYesNo("Use AI for crossover? (Y/n): ", true);

    if (!useAIPopulation && !useAICrossover)
    {
        TravelingSalesman.RunWithAG(gaSettings, cityCount);
        return;
    }

    var aiProvider = GetAIProvider(appConfiguration, gaSettings.PopulationSize);
    TravelingSalesman.RunWithAIOptions(aiProvider, gaSettings, useAIPopulation, useAICrossover, cityCount);
}

static int AskCityCount(IConfiguration configuration)
{
    int defaultCityCount = int.TryParse(configuration["TravelingSalesman:CityCount"], out var configured) && configured >= TravelingSalesmanProblem.MinCityCount
        ? configured
        : TravelingSalesmanProblem.DefaultCityCount;

    while (true)
    {
        Console.Write($"Number of cities (min {TravelingSalesmanProblem.MinCityCount}, default {defaultCityCount}): ");
        var input = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(input))
            return defaultCityCount;

        if (int.TryParse(input, out var cityCount) && cityCount >= TravelingSalesmanProblem.MinCityCount)
            return cityCount;

        Console.WriteLine($"Invalid value. Enter an integer >= {TravelingSalesmanProblem.MinCityCount}.");
    }
}

static bool AskYesNo(string prompt, bool defaultValue)
{
    Console.Write(prompt);
    var input = Console.ReadLine()?.Trim();

    if (string.IsNullOrWhiteSpace(input))
        return defaultValue;

    input = input.ToLowerInvariant();
    return input == "y" || input == "yes" || input == "1";
}



static IAIProvider GetAIProvider(IConfiguration configuration, int populationSize)
{
    Console.WriteLine("Choose an AI provider:");
    Console.WriteLine(" 1) OpenAI");
    Console.WriteLine(" 2) DeepSeek");
    Console.WriteLine(" 3) LM Studio (OpenAI-compatible local API)");
    Console.WriteLine(" 4) Claude (Claude Code CLI, uses your Claude subscription)");
    Console.Write("Option (1-4, default 4): ");
    var providerOption = Console.ReadLine()?.Trim();
    if (string.IsNullOrWhiteSpace(providerOption))
        providerOption = "4";

    if (providerOption == "4")
        return CreateClaudeCodeProvider(configuration);

    if (providerOption == "3")
        return CreateLmStudioProvider(configuration);

    if (providerOption == "2")
        return CreateDeepSeekProvider(configuration);

    return CreateOpenAIProvider(configuration, populationSize);
}

static IAIProvider CreateOpenAIProvider(IConfiguration configuration, int populationSize)
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
    Console.WriteLine(" 3) deepseek-v4-pro");
    Console.WriteLine(" 4) deepseek-v4-flash");
    
    Console.Write("Option (1-4, default 1): ");
    var option = Console.ReadLine()?.Trim();

    string model = option switch
    {
        "2" => "deepseek-reasoner",
        "3" => "deepseek-v4-pro",
        "4" => "deepseek-v4-flash",
        _ => "deepseek-chat"
    };

    return CreateDeepSeekProviderWithModel(configuration, model);
}

static IAIProvider CreateClaudeCodeProvider(IConfiguration configuration)
{
    string? cliPath = configuration["Claude:CliPath"];
    int timeoutSeconds = int.TryParse(configuration["Claude:TimeoutSeconds"], out var seconds) && seconds > 0 ? seconds : 600;

    string defaultModel = configuration["Claude:Model"] is { Length: > 0 } configuredModel ? configuredModel : "opus";

    Console.WriteLine("Choose a model:");
    Console.WriteLine(" 1) sonnet  (latest Sonnet)");
    Console.WriteLine(" 2) opus    (latest Opus)");
    Console.WriteLine(" 3) haiku   (latest Haiku, fastest)");
    Console.WriteLine(" 4) fable   (latest Fable)");
    Console.WriteLine(" 5) Other (enter full model name, e.g. claude-opus-5-5)");
    Console.Write($"Option (1-5, default {defaultModel}): ");
    var option = Console.ReadLine()?.Trim();

    string model = option switch
    {
        "1" => "sonnet",
        "2" => "opus",
        "3" => "haiku",
        "4" => "fable",
        "5" => ReadCustomClaudeModel(defaultModel),
        _ => defaultModel
    };

    Console.WriteLine($"Using Claude model: {model}");
    return new ClaudeCodeProvider(model, cliPath, TimeSpan.FromSeconds(timeoutSeconds));
}

static string ReadCustomClaudeModel(string defaultModel)
{
    Console.Write("Model name: ");
    var model = Console.ReadLine()?.Trim();
    return string.IsNullOrWhiteSpace(model) ? defaultModel : model;
}

static IAIProvider CreateLmStudioProvider(IConfiguration configuration)
{
    string baseUrl = configuration["LMStudio:BaseUrl"] ?? "http://localhost:1234/v1";
    string? apiKey = configuration["LMStudio:ApiKey"] ?? "sk-lm-mrpJFc9W:tpjJ8TAMJGZG1wYKJBkJ";
    

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

    return new LMStudioProvider(baseUrl, model, apiKey);
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
