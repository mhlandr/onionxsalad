using Microsoft.AspNetCore.Mvc;
using onion.Areas.Identity.Data;
using onion.Models;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using onion.Services;

[ApiController]
[Route("api/[controller]")]
public class ScreenshotController : ControllerBase
{
    private readonly IQueueService _queueService;
    private readonly AuthSystemDbContext _dbContext;
    private readonly UserManager<AppUser> _userManager;

    // Ensure there is only one constructor
    public ScreenshotController(
        IQueueService queueService,
        AuthSystemDbContext dbContext,
        UserManager<AppUser> userManager)
    {
        _queueService = queueService;
        _dbContext = dbContext;
        _userManager = userManager;
    }

    [HttpPost("capture")]
    public async Task<IActionResult> EnqueueScreenshot([FromBody] ScreenshotPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload?.Url))
        {
            return BadRequest(new { error = "Invalid or missing URL" });
        }

        try
        {
            var requestId = _queueService.EnqueueScreenshotRequest(payload.Url);

            // Extract User ID and IP Address
            string userId = User.Identity.IsAuthenticated ? _userManager.GetUserId(User) : null;
            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Log the request in the database
            var logEntry = new ScreenshotRequestLog
            {
                RequestId = requestId,
                Url = payload.Url,
                RequestedAt = DateTime.UtcNow,
                UserId = userId,
                IPAddress = ipAddress,
                Status = "Pending" // Set initial status
                                   // ErrorMessage is left as null
            };

            _dbContext.ScreenshotRequestLogs.Add(logEntry);
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Screenshot request enqueued", requestId });
        }
        catch (Exception ex)
        {
            // Log the exception details (you can use a logging framework)
            Console.WriteLine($"Exception in EnqueueScreenshot: {ex.Message}\n{ex.StackTrace}");
            return StatusCode(500, new { error = "An error occurred while processing your request." });
        }
    }

    [HttpGet("status/{id}")]
    public IActionResult GetScreenshotStatus(Guid id)
    {
        var request = _queueService.GetRequestStatus(id);
        if (request == null)
        {
            return NotFound(new { error = "Request not found" });
        }

        string screenshotUrl = null;
        if (request.Status == "Completed")
        {
            screenshotUrl = Url.Content($"~/{request.ScreenshotPath}");
        }

        var response = new
        {
            id = request.Id,
            status = request.Status,
            screenshotPath = screenshotUrl,
            errorMessage = request.ErrorMessage
        };

        return Ok(response);
    }
}
