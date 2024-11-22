using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using onion.Models;
using onion.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace onion.Controllers
{
    public class ForensicsToolsController : Controller
    {
        private readonly IImageProcessingService _imageProcessingService;
        private readonly ILogger<ForensicsToolsController> _logger;

        public ForensicsToolsController(IImageProcessingService imageProcessingService, ILogger<ForensicsToolsController> logger)
        {
            _imageProcessingService = imageProcessingService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult ForensicsTools()
        {
            // Return an empty model on GET requests
            return View(new ForensicsToolsViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForensicsTools(ForensicsToolsViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(model.WebsiteUrl))
                {
                    ModelState.AddModelError("WebsiteUrl", "Website URL is required.");
                    return View(model);
                }

                try
                {
                    _logger.LogInformation("Starting analysis for URL: {WebsiteUrl}", model.WebsiteUrl);

                    // Download website and analyze images
                    string downloadDirectory = Path.Combine(Path.GetTempPath(), "downloaded_website");

                    (string downloadedHtmlPath, List<ImageData> analyzedImages) = await _imageProcessingService.DownloadWebsiteAndAnalyzeImagesAsync(model.WebsiteUrl, downloadDirectory);

                    _logger.LogInformation("Downloaded HTML Path: {DownloadedHtmlPath}", downloadedHtmlPath);
                    _logger.LogInformation("Analyzed Images Count: {Count}", analyzedImages.Count);

                    // Set the analysis results in the model
                    model.AnalyzedImages = analyzedImages;

                    // Indicate that a POST has occurred
                    model.IsPost = true;

                    return View(model);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while analyzing images.");
                    ModelState.AddModelError(string.Empty, "An error occurred while analyzing images. Details: " + ex.Message);
                    return View(model);
                }
            }
            else
            {
                // Return the view with validation errors
                return View(model);
            }
        }
    }
}
