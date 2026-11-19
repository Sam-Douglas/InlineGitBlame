using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace OpenGitBlame.Git;

internal static class GitProcess
{
    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// Runs git and returns what it wrote to standard output, or null if it failed or isn't installed.
    /// </summary>
    public static async Task<string?> RunAsync(string workingDirectory, string arguments, string? input = null)
    {
        var startInfo = new ProcessStartInfo("git", arguments)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = input != null,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Utf8,
            StandardErrorEncoding = Utf8,
        };

        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();
        }
        catch (Win32Exception ex)
        {
            Debug.WriteLine($"OpenGitBlame: could not start git: {ex.Message}");
            return null;
        }

        // Read both streams before writing input so git never blocks on a full pipe.
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();

        if (input != null)
        {
            byte[] bytes = Utf8.GetBytes(input);
            await process.StandardInput.BaseStream.WriteAsync(bytes, 0, bytes.Length);
            process.StandardInput.Close();
        }

        string result = await output;
        string errorText = await error;
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            Debug.WriteLine($"OpenGitBlame: git {arguments} failed: {errorText.Trim()}");
            return null;
        }

        return result;
    }
}
