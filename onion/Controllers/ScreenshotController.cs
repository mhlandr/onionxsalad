using Microsoft.AspNetCore.Mvc;
using onion.Models;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class ScreenshotController : ControllerBase
{
    private readonly ScreenshotService _screenshotService;

    public ScreenshotController(ScreenshotService screenshotService)
    {
        _screenshotService = screenshotService;
    }

    [HttpPost("capture")]
    public async Task<IActionResult> CaptureOnionScreenshot([FromBody] ScreenshotPayload payload)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(payload?.Url))
            {
                return BadRequest(new { error = "Invalid or missing URL" });
            }

            string screenshotPath = await _screenshotService.CaptureOnionScreenshotAsync(payload.Url);
            string relativePath = Url.Content($"~/screenshots/{Path.GetFileName(screenshotPath)}");  // Ensure correct path format for browser
            return Ok(new { message = "Screenshot captured", path = relativePath });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in CaptureOnionScreenshot: {ex.Message}\n{ex.StackTrace}");
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }


}
