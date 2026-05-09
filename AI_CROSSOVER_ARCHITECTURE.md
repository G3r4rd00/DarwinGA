# AI-Powered Genetic Algorithm Crossover

This implementation extends DarwinGA with AI-powered population crossover capabilities.

## Architecture

### 1. AI Provider Interface (`DarwinGA/AI/IAIProvider.cs`)

Generic interface for AI service providers:

```csharp
public interface IAIProvider
{
    Task<string> SendPromptAsync(string prompt);
}
```

This allows you to implement different AI providers (ChatGPT, Claude, Gemini, etc.)

### 2. ChatGPT Provider (`DarwinGA/AI/ChatGPTProvider.cs`)

Implementation for OpenAI's ChatGPT:

```csharp
var provider = new ChatGPTProvider(apiKey: "sk-...", model: "gpt-3.5-turbo");
```

Features:
- Uses OpenAI Chat Completions API
- Configurable model selection
- Proper error handling
- System prompt optimized for genetic algorithms

### 3. AI Crossover Operator (`DarwinGA/Evolutionals/BinaryEvolutional/Crossers/AICrosser.cs`)

Population-wide crossover using AI:

```csharp
public class AICrosser : IPopulationCrosser<BinaryEvolutional>
{
    public AICrosser(IAIProvider aiProvider)
    {
        // ...
    }
}
```

**How it works:**
1. Serializes entire population to JSON
2. Sends to AI with specific prompt
3. AI performs crossover operations
4. Deserializes offspring population
5. Returns new generation

### 4. Configuration Management

**Security-first approach:**

- `appsettings.json` - Your actual API key (git-ignored)
- `appsettings.Example.json` - Template for other developers
- `.gitignore` - Excludes sensitive configuration files

## Usage Example

```csharp
// 1. Load configuration
var config = LoadConfiguration();

// 2. Create AI provider
var aiProvider = new ChatGPTProvider(config.ApiKey, config.Model);

// 3. Configure genetic algorithm
var ga = new GeneticAlgorithm<BinaryEvolutional>
{
    PopulationCrosser = new AICrosser(aiProvider),
    Cross = null, // Not needed with PopulationCrosser
    // ... other settings
};

// 4. Run evolution
ga.Run(populationSize);
```

## Setup Instructions

1. **Copy configuration template:**
   ```bash
   cd DarwinGA.Example
   cp appsettings.Example.json appsettings.json
   ```

2. **Get OpenAI API key:**
   - Visit https://platform.openai.com/api-keys
   - Create new key
   - Copy key to clipboard

3. **Update configuration:**
   Edit `appsettings.json`:
   ```json
   {
     "AI": {
       "Provider": "ChatGPT",
       "ChatGPT": {
         "ApiKey": "sk-your-key-here",
         "Model": "gpt-3.5-turbo"
       }
     }
   }
   ```

4. **Run the example:**
   ```bash
   dotnet run --project DarwinGA.Example
   # Choose option 5 from menu
   ```

## Adding New AI Providers

To add support for a new AI service:

1. **Create provider class:**
   ```csharp
   public class MyAIProvider : IAIProvider
   {
       public async Task<string> SendPromptAsync(string prompt)
       {
           // Your implementation
       }
   }
   ```

2. **Update configuration schema:**
   ```json
   {
     "AI": {
       "Provider": "MyAI",
       "MyAI": {
         "ApiKey": "...",
         "Endpoint": "...",
         // ... other settings
       }
     }
   }
   ```

3. **Use in examples:**
   ```csharp
   var provider = new MyAIProvider(config.ApiKey);
   var crosser = new AICrosser(provider);
   ```

## Cost Considerations

**API calls per generation:** 1

For a typical run:
- Population size: 10-20 individuals
- Generations: 5-10
- Total API calls: 5-10
- Estimated cost: $0.001-0.01 USD (with gpt-3.5-turbo)

**Tips:**
- Start with small populations for testing
- Use `gpt-3.5-turbo` for development
- Monitor usage at https://platform.openai.com/usage
- Consider caching for identical populations

## Design Benefits

1. **Flexibility:** Easy to swap AI providers
2. **Security:** API keys never in source control
3. **Extensibility:** New providers without changing core code
4. **Compatibility:** Works alongside traditional crossover operators
5. **Type-safety:** Generic interface for any chromosome type

## Files Created/Modified

```
DarwinGA/
├── AI/
│   ├── IAIProvider.cs              (new)
│   └── ChatGPTProvider.cs          (new)
└── Evolutionals/BinaryEvolutional/Crossers/
    └── AICrosser.cs                (modified)

DarwinGA.Example/
├── Examples/
│   └── Example05_AICrosser.cs      (modified)
├── appsettings.json                (new, git-ignored)
├── appsettings.Example.json        (new, template)
└── README_AI_CONFIG.md             (new)

.gitignore                          (modified)
```

## Next Steps

- [ ] Implement async/await throughout GA pipeline
- [ ] Add response caching to reduce API calls
- [ ] Create adapters for other chromosome types (not just BinaryEvolutional)
- [ ] Add support for other AI providers (Claude, Gemini, local LLMs)
- [ ] Implement batch processing for multiple generations
- [ ] Add telemetry for AI call performance
