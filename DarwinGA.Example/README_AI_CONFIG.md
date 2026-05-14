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


## Available Models (June 2024)

You can use the following ChatGPT models (see https://developers.openai.com/api/docs/models/all):

- `gpt-3.5-turbo`
- `gpt-3.5-turbo-0125`
- `gpt-3.5-turbo-1106`
- `gpt-3.5-turbo-0613`
- `gpt-3.5-turbo-16k`
- `gpt-4`
- `gpt-4-0613`
- `gpt-4-32k`
- `gpt-4-32k-0613`
- `gpt-4-turbo`
- `gpt-4-0125-preview`
- `gpt-4-1106-preview`
- `gpt-4o`

**Note:** Some models (like `gpt-4o`, `gpt-4-turbo`, `gpt-4-0125-preview`, `gpt-4-1106-preview`) do NOT support the `temperature` parameter. The system will automatically omit it for those models.

## Cost Considerations

Each generation makes one API call to OpenAI. Consider:
- Start with small populations (10-20 individuals)
- Use few generations (5-10) for testing
- Monitor your OpenAI usage at https://platform.openai.com/usage
