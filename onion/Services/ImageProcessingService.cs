using HtmlAgilityPack;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.FileSystem;
using onion.Models;

namespace onion.Services
{
    public class ImageProcessingService : IImageProcessingService
    {
        private readonly HttpClient _httpClient;

        public ImageProcessingService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<(string HtmlPath, List<ImageData> AnalyzedImages)> DownloadWebsiteAndAnalyzeImagesAsync(string websiteUrl, string downloadDirectory)
        {
            // Fetch the main HTML content
            var response = await _httpClient.GetAsync(websiteUrl);
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to fetch website: {response.StatusCode}");

            var htmlContent = await response.Content.ReadAsStringAsync();

            // Initialize HtmlDocument
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);

            // Set the download directory to wwwroot/downloaded_website
            string wwwrootPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot");
            downloadDirectory = Path.Combine(wwwrootPath, "downloaded_website");

            // Delete the directory if it exists to avoid old images
            if (System.IO.Directory.Exists(downloadDirectory))
            {
                System.IO.Directory.Delete(downloadDirectory, true);
            }

            System.IO.Directory.CreateDirectory(downloadDirectory);

            var imagesDirectory = Path.Combine(downloadDirectory, "images");
            System.IO.Directory.CreateDirectory(imagesDirectory);

            // Process and download images
            var imgNodes = htmlDoc.DocumentNode.SelectNodes("//img[@src]");
            var imagesData = new List<ImageData>();

            if (imgNodes != null)
            {
                foreach (var img in imgNodes)
                {
                    var src = img.GetAttributeValue("src", null);
                    if (string.IsNullOrEmpty(src))
                        continue;

                    // Handle relative URLs
                    var imageUrl = new Uri(new Uri(websiteUrl), src).ToString();

                    try
                    {
                        // Download the image and save locally
                        var imageResponse = await _httpClient.GetAsync(imageUrl);
                        if (!imageResponse.IsSuccessStatusCode)
                            continue;

                        var imageBytes = await imageResponse.Content.ReadAsByteArrayAsync();

                        // Generate a unique file name to avoid conflicts
                        var fileExtension = Path.GetExtension(new Uri(imageUrl).LocalPath);
                        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                        var localImagePath = Path.Combine(imagesDirectory, uniqueFileName);

                        await File.WriteAllBytesAsync(localImagePath, imageBytes);

                        // Update the src attribute in the HTML to the local path
                        img.SetAttributeValue("src", $"/downloaded_website/images/{uniqueFileName}");

                        // Extract metadata from the downloaded image
                        var metadata = ExtractMetadata(imageBytes);

                        imagesData.Add(new ImageData
                        {
                            ImageUrl = imageUrl,
                            LocalFileName = uniqueFileName,
                            Metadata = metadata,
                            ImageBytes = imageBytes
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error downloading image {imageUrl}: {ex.Message}");
                    }
                }
            }

            // Save modified HTML locally
            var localHtmlPath = Path.Combine(downloadDirectory, "index.html");
            await File.WriteAllTextAsync(localHtmlPath, htmlDoc.DocumentNode.OuterHtml);

            return (localHtmlPath, imagesData); // Return both the path of the downloaded HTML and the analyzed images
        }

        private Dictionary<string, string> ExtractMetadata(byte[] imageBytes)
        {
            var metadataDict = new Dictionary<string, string>();

            using (var stream = new MemoryStream(imageBytes))
            {
                var directories = ImageMetadataReader.ReadMetadata(stream);

                foreach (var directory in directories)
                {
                    foreach (var tag in directory.Tags)
                    {
                        // To avoid duplicates, check if key exists
                        var key = $"{directory.Name} - {tag.Name}";
                        if (!metadataDict.ContainsKey(key))
                        {
                            metadataDict[key] = tag.Description;
                        }
                    }

                    // Handle specific directories for more detailed data
                    if (directory is GpsDirectory gpsDirectory)
                    {
                        var location = gpsDirectory.GetGeoLocation();
                        if (location != null)
                        {
                            metadataDict["GPS Latitude"] = location.Latitude.ToString();
                            metadataDict["GPS Longitude"] = location.Longitude.ToString();
                        }
                    }
                    else if (directory is ExifSubIfdDirectory exifDirectory)
                    {
                        var dateTimeOriginal = exifDirectory.GetDateTime(ExifDirectoryBase.TagDateTimeOriginal);
                        if (dateTimeOriginal != null)
                        {
                            metadataDict["EXIF - Date/Time Original"] = dateTimeOriginal.ToString();
                        }
                    }
                    else if (directory is FileMetadataDirectory fileMetadataDirectory)
                    {
                        // Extract file metadata
                        foreach (var tag in fileMetadataDirectory.Tags)
                        {
                            var key = $"{directory.Name} - {tag.Name}";
                            if (!metadataDict.ContainsKey(key))
                            {
                                metadataDict[key] = tag.Description;
                            }
                        }
                    }
                }
            }

            return metadataDict;
        }
    }
}
