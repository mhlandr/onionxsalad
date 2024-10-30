using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using onion.Areas.Identity.Data;
using System.Threading;
using System.Threading.Tasks;


namespace onion.Services
{
    public class ScreenshotBackgroundService : BackgroundService
    {
        private readonly IQueueService _queueService;
        private readonly ScreenshotService _screenshotService;
        private readonly IServiceScopeFactory _scopeFactory;

        public ScreenshotBackgroundService(
            IQueueService queueService,
            ScreenshotService screenshotService,
            IServiceScopeFactory scopeFactory)
        {
            _queueService = queueService;
            _screenshotService = screenshotService;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_queueService.TryDequeue(out var request))
                {
                    request.Status = "Processing";

                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<AuthSystemDbContext>();

                        // Update status in the database
                        var logEntry = await dbContext.ScreenshotRequestLogs
                            .FirstOrDefaultAsync(l => l.RequestId == request.Id);

                        if (logEntry != null)
                        {
                            logEntry.Status = "Processing";
                            await dbContext.SaveChangesAsync();
                        }

                        try
                        {
                            string screenshotPath = await _screenshotService.CaptureOnionScreenshotAsync(request.Url);
                            request.ScreenshotPath = screenshotPath;
                            request.Status = "Completed";

                            if (logEntry != null)
                            {
                                logEntry.Status = "Completed";
                                logEntry.ScreenshotPath = screenshotPath;
                                await dbContext.SaveChangesAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            request.Status = "Failed";
                            request.ErrorMessage = ex.Message;

                            if (logEntry != null)
                            {
                                logEntry.Status = "Failed";
                                logEntry.ErrorMessage = ex.Message;
                                await dbContext.SaveChangesAsync();
                            }
                        }
                    }
                }
                else
                {
                    await Task.Delay(1000, stoppingToken); // Wait before checking the queue again
                }
            }
        }
    }

}
