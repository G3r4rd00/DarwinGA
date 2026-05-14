# AI Crossover Configuration

## Setup

1. **Choose how to provide credentials:**
   - Add keys to `appsettings.json`, or
   - Set environment variables (`OPENAI_API_KEY` / `DEEPSEEK_API_KEY`).

   The example reads `appsettings.json`, `appsettings.debug.json`, or `appsettings.Debug.json` from the output folder. Keep real secrets out of source control.

2. **Edit `appsettings.json` and add the API key for the provider you want to use:**
   ```json
   {
     "OpenAI": {
       "ApiKey": "sk-your-openai-api-key-here"
     },
     "DeepSeek": {
       "ApiKey": "sk-your-deepseek-api-key-here",
       "BaseUrl": "https://api.deepseek.com"
     }
   }
   ```

   You can also use environment variables instead of storing keys in `appsettings.json`:
   - `OPENAI_API_KEY`
   - `DEEPSEEK_API_KEY`

3. **Get your API key:**
   - OpenAI:
     - Go to https://platform.openai.com/api-keys
     - Create a new API key
     - Copy and paste it into your `appsettings.json`, or set `OPENAI_API_KEY`
   - DeepSeek:
     - Go to https://platform.deepseek.com/api_keys
     - Create a new API key
     - Copy and paste it into your `appsettings.json`, or set `DEEPSEEK_API_KEY`

## Security

- The `appsettings.json` file is excluded from Git via `.gitignore`
- Never commit your API key to the repository
- Use a sanitized settings file as a template for other developers

## Usage

Run Example 5 from the menu to see the AI-based crossover in action:

```
Choose an example:
...
5. AI-based Population Crossover
...
```

The example then asks which provider to use:

```text
Choose an AI provider:
 1) OpenAI
 2) DeepSeek
```

For DeepSeek, the example offers:

```text
Choose a model:
 1) deepseek-chat
 2) deepseek-reasoner
```


## Available Models

### OpenAI

You can use the following OpenAI models from the example menu:

- `gpt-5.5`
- `gpt-5.5-pro`
- `gpt-5.4`
- `gpt-4o`
- `gpt-4`
- `gpt-3.5-turbo`

### DeepSeek

You can use the following DeepSeek models from the example menu:

- `deepseek-chat`
- `deepseek-reasoner`

`DeepSeekProvider` uses `https://api.deepseek.com` by default. Override it with `DeepSeek:BaseUrl` if needed.

## Cost Considerations

Each generation can make one API call to the selected provider. Consider:
- Start with small populations (10-20 individuals)
- Use few generations (5-10) for testing
- Monitor OpenAI usage at https://platform.openai.com/usage
- Monitor DeepSeek usage in the DeepSeek platform dashboard
