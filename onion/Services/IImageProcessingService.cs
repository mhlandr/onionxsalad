using onion.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace onion.Services
{
    public interface IImageProcessingService
    {
        Task<(string HtmlPath, List<ImageData> AnalyzedImages)> DownloadWebsiteAndAnalyzeImagesAsync(string websiteUrl, string downloadDirectory);
    }
}
