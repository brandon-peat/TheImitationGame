using System.Diagnostics;
using System.Text.Json;
using TheImitationGame.Api.Interfaces;

namespace TheImitationGame.Api.Services
{
    public class ImitationGenerator : IImitationGenerator
    {
        public async Task<List<string>> GenerateImitations(string prompt, string image_b64, int amount)
        {
            var start = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = @"..\TheImitationGame.Image\cli.py",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = start };
            process.Start();

            var input = new
            {
                prompt,
                image_b64,
                amount
            };
            string jsonInput = JsonSerializer.Serialize(input);
            await process.StandardInput.WriteAsync(jsonInput);
            process.StandardInput.Close();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await Task.WhenAll(outputTask, errorTask);

            process.WaitForExit();

            string output = outputTask.Result;
            string errors = errorTask.Result;

            if (process.ExitCode != 0)
                throw new Exception("Python error: " + errors);

            var result = JsonSerializer.Deserialize<List<string>>(output);
            return result ?? throw new Exception("Failed to parse Python output.");
        }
    }
}
