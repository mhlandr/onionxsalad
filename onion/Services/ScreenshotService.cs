using System.Diagnostics;

public class ScreenshotService
{
    public async Task<string> CaptureOnionScreenshotAsync(string url)
    {
        // Ensure the URL starts with 'http://' or 'https://'
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
        {
            url = "http://" + url;
        }

        // Paths
        string nodeScriptPath = @"wwwroot/scripts/capture_screenshot.js";  // Path to your Node.js script
        string screenshotDirectory = @"wwwroot/screenshots";  // Directory to save screenshots
        string screenshotFileName = $"screenshot_{DateTime.Now:yyyyMMddHHmmss}.png";
        string screenshotPath = Path.Combine(screenshotDirectory, screenshotFileName);

        // Ensure screenshot directory exists
        if (!Directory.Exists(screenshotDirectory))
        {
            Directory.CreateDirectory(screenshotDirectory);
        }

        // Prepare process start info for Node.js script
        var startInfo = new ProcessStartInfo
        {
            FileName = @"C:\Program Files\nodejs\node.exe",  // Full path to Node.js
            Arguments = $"{nodeScriptPath} {url} {screenshotPath}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };


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

                // Check for successful execution
                if (process.ExitCode == 0)
                {
                    Console.WriteLine($"Screenshot saved at: {screenshotPath}");
                    return screenshotPath;  // Return the screenshot file path
                }
                else
                {
                    // Log the error from Node.js process
                    Console.WriteLine($"Node.js Error: {error}");
                    throw new Exception($"Screenshot capture failed: {error}");
                }
            }
        }
        catch (Exception ex)
        {
            // Log any exceptions that occur
            Console.WriteLine($"Exception in ScreenshotService: {ex.Message}");
            throw;
        }
    }
}
