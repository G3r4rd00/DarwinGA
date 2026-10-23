using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace DarwinGA.AI
{
    /// <summary>
    /// Claude provider that runs the locally installed Claude Code CLI (<c>claude -p</c>) in headless mode.
    /// It uses the account the CLI is logged in with (Claude Pro/Max subscription), so no API key is needed.
    /// Requires Claude Code to be installed and authenticated (run <c>claude</c> once and log in).
    /// </summary>
    public class ClaudeCodeProvider : AIProviderBase
    {
        private readonly string _cliPath;
        private readonly string _model;
        private readonly TimeSpan _timeout;
        private string _systemMessage = string.Empty;

        /// <summary>
        /// Creates a new Claude Code provider.
        /// </summary>
        /// <param name="model">Model alias or full name accepted by <c>claude --model</c> (for example: sonnet, opus, haiku).</param>
        /// <param name="cliPath">Optional path to the claude executable. When empty it is searched in PATH.</param>
        /// <param name="timeout">Maximum time to wait for each response (default: 10 minutes).</param>
        /// <param name="systemMessage">Optional system message.</param>
        public ClaudeCodeProvider(string model = "sonnet", string? cliPath = null, TimeSpan? timeout = null, string systemMessage = "")
        {
            if (string.IsNullOrWhiteSpace(model))
                throw new ArgumentException("Model cannot be null or empty.", nameof(model));

            _model = model;
            _cliPath = string.IsNullOrWhiteSpace(cliPath) ? ResolveCliPath() : cliPath;
            _timeout = timeout ?? TimeSpan.FromMinutes(10);

            if (!string.IsNullOrWhiteSpace(systemMessage))
                _systemMessage = systemMessage;
        }

        public override void SetSystemMessage(string systemMessage)
        {
            if (string.IsNullOrWhiteSpace(systemMessage))
                throw new ArgumentException("System message cannot be null or empty.", nameof(systemMessage));
            _systemMessage = systemMessage;
        }

        public override async Task<string> SendPromptAsync(string prompt)
        {
            // The system message goes through a temp file and the prompt through stdin
            // to avoid command-line length and quoting limits.
            string? systemPromptFile = null;
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = _cliPath,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardInputEncoding = new UTF8Encoding(false),
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                    // Run outside the project so no CLAUDE.md or project settings are picked up.
                    WorkingDirectory = Path.GetTempPath()
                };

                startInfo.ArgumentList.Add("-p");
                startInfo.ArgumentList.Add("--output-format");
                startInfo.ArgumentList.Add("json");
                startInfo.ArgumentList.Add("--model");
                startInfo.ArgumentList.Add(_model);
                startInfo.ArgumentList.Add("--tools");
                startInfo.ArgumentList.Add("");
                startInfo.ArgumentList.Add("--strict-mcp-config");
                startInfo.ArgumentList.Add("--no-session-persistence");

                if (!string.IsNullOrWhiteSpace(_systemMessage))
                {
                    systemPromptFile = Path.Combine(Path.GetTempPath(), $"darwinga-system-{Guid.NewGuid():N}.txt");
                    await File.WriteAllTextAsync(systemPromptFile, _systemMessage, new UTF8Encoding(false));
                    startInfo.ArgumentList.Add("--system-prompt-file");
                    startInfo.ArgumentList.Add(systemPromptFile);
                }

                // Force subscription auth: an API key in the environment would take precedence over the logged-in account.
                startInfo.Environment.Remove("ANTHROPIC_API_KEY");

                using var process = new Process { StartInfo = startInfo };
                try
                {
                    process.Start();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Could not start Claude Code CLI at '{_cliPath}'. Install it (npm install -g @anthropic-ai/claude-code) and log in by running 'claude'.", ex);
                }

                var stdoutTask = process.StandardOutput.ReadToEndAsync();
                var stderrTask = process.StandardError.ReadToEndAsync();

                await process.StandardInput.WriteAsync(prompt);
                process.StandardInput.Close();

                using var cts = new CancellationTokenSource(_timeout);
                try
                {
                    await process.WaitForExitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    process.Kill(entireProcessTree: true);
                    throw new TimeoutException($"Claude Code did not respond within {_timeout.TotalSeconds:0} seconds.");
                }

                var stdout = await stdoutTask;
                var stderr = await stderrTask;

                var text = ExtractResult(stdout, stderr, process.ExitCode);
                return ParseResponse(text);
            }
            finally
            {
                if (systemPromptFile != null && File.Exists(systemPromptFile))
                    File.Delete(systemPromptFile);
            }
        }

        private static string ExtractResult(string stdout, string stderr, int exitCode)
        {
            if (string.IsNullOrWhiteSpace(stdout))
                throw new InvalidOperationException($"Claude Code returned no output (exit code {exitCode}). {stderr}".Trim());

            using var document = JsonDocument.Parse(stdout);
            var root = document.RootElement;

            var result = root.TryGetProperty("result", out var resultElement) && resultElement.ValueKind == JsonValueKind.String
                ? resultElement.GetString()
                : null;

            bool isError = root.TryGetProperty("is_error", out var isErrorElement) && isErrorElement.ValueKind == JsonValueKind.True;
            if (isError || exitCode != 0)
                throw new InvalidOperationException($"Claude Code error (exit code {exitCode}): {result ?? stderr}".Trim());

            if (string.IsNullOrWhiteSpace(result))
                throw new InvalidOperationException("Claude Code response did not include a result.");

            return result;
        }

        private static string ResolveCliPath()
        {
            var candidates = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? new[] { "claude.exe", "claude.cmd" }
                : new[] { "claude" };

            var searchDirs = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Append(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin"));

            foreach (var dir in searchDirs)
            {
                foreach (var candidate in candidates)
                {
                    var fullPath = Path.Combine(dir.Trim(), candidate);
                    if (File.Exists(fullPath))
                        return fullPath;
                }
            }

            return candidates[0];
        }
    }
}
