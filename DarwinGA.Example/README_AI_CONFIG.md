# AI Crossover Configuration

## Setup

1. **Copy the example configuration file:**
   ```bash
   cp appsettings.Example.json appsettings.json
   ```

2. **Edit `appsettings.json` and add your OpenAI API key:**
   ```json
   {
     "AI": {
       "Provider": "ChatGPT",
       "ChatGPT": {
         "ApiKey": "sk-your-actual-api-key-here",
         "Model": "gpt-3.5-turbo"
       }
     }
   }
   ```

3. **Get your API key from OpenAI:**
   - Go to https://platform.openai.com/api-keys
   - Create a new API key
   - Copy and paste it into your `appsettings.json`

## Security

- The `appsettings.json` file is excluded from Git via `.gitignore`
- Never commit your API key to the repository
- Use `appsettings.Example.json` as a template for other developers

## Usage

Run Example 5 from the menu to see the AI-based crossover in action:

```
Choose an example:
...
5. AI-based Population Crossover (ChatGPT)
...
```

## Available Models

You can use different ChatGPT models:
- `gpt-3.5-turbo` (faster, cheaper)
- `gpt-4` (more capable, slower, more expensive)
- `gpt-4-turbo` (good balance)

## Cost Considerations

Each generation makes one API call to OpenAI. Consider:
- Start with small populations (10-20 individuals)
- Use few generations (5-10) for testing
- Monitor your OpenAI usage at https://platform.openai.com/usage
