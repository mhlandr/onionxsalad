using System.Diagnostics;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Elfie.Model.Tree;

public class ScreenshotService
{
    public async Task<string> CaptureOnionScreenshotAsync(string url)
    {
        // Ensure the URL starts with 'http://' or 'https://'
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
        {
            url = "http://" + url;
        }

        // Define the base directory for the project
        string baseDirectory = @"C:\Users\mhlan\source\repos\onion\onionxsalad\onion";

        // Paths
        string nodeScriptPath = Path.Combine(baseDirectory, "wwwroot", "scripts", "capture_screenshot.js");  // Path to your Node.js script
        string screenshotDirectory = Path.Combine(baseDirectory, "wwwroot", "screenshots");  // Directory to save screenshots
        string screenshotFileName = $"screenshot_{DateTime.Now:yyyyMMddHHmmss}.png";
        string screenshotFullPath = Path.Combine(screenshotDirectory, screenshotFileName);

        // Ensure screenshot directory exists
        if (!Directory.Exists(screenshotDirectory))
        {
            Directory.CreateDirectory(screenshotDirectory);
        }

        // Prepare process start info for Node.js script
        var startInfo = new ProcessStartInfo
        {
            FileName = @"C:\Program Files\nodejs\node.exe",  // Full path to Node.js executable
            Arguments = $"\"{nodeScriptPath}\" \"{url}\" \"{screenshotFullPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Log the full paths and URLs
        Console.WriteLine($"Node.js executable path: {startInfo.FileName}");
        Console.WriteLine($"Node.js script path: {nodeScriptPath}");
        Console.WriteLine($"URL to capture: {url}");
        Console.WriteLine($"Screenshot will be saved to: {screenshotFullPath}");

        try
        {
            using (var process = new Process { StartInfo = startInfo })
            {
                process.Start();

                // Read output and error streams
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();

                // Wait for the process to exit
                await process.WaitForExitAsync();

                // Log Node.js process output
                Console.WriteLine("Node.js Standard Output:");
                Console.WriteLine(output);
                Console.WriteLine("Node.js Standard Error:");
                Console.WriteLine(error);

                // Check for successful execution
                if (process.ExitCode == 0)
                {
                    Console.WriteLine($"Screenshot saved at: {screenshotFullPath}");

                    // Verify if the file exists
                    if (File.Exists(screenshotFullPath))
                    {
                        Console.WriteLine("File exists after saving.");
                    }
                    else
                    {
                        Console.WriteLine("File does not exist after supposed saving.");
                    }

                    // Return the relative path to the screenshot
                    string screenshotRelativePath = screenshotFullPath.Replace("\\", "/").Replace(baseDirectory.Replace("\\", "/") + "/wwwroot/", "");
                    return screenshotRelativePath;
                }
                else
                {
                    // Log the error from Node.js process
                    Console.WriteLine($"Node.js process exited with code {process.ExitCode}");
                    throw new Exception($"Screenshot capture failed: {error}");
                }
            }
        }
        catch (Exception ex)
        {
            // Log any exceptions that occur
            Console.WriteLine($"Exception in ScreenshotService: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
    }
}

