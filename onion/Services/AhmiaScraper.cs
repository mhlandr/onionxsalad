using HtmlAgilityPack;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

public class AhmiaScraperService
{
    private readonly IMongoCollection<BsonDocument> _collection;

    public AhmiaScraperService(IMongoCollection<BsonDocument> collection)
    {
        _collection = collection;
    }

    // Helper method to normalize URLs (get base domain)
    private string NormalizeUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            return uri.Host;  // Return the host part 
        }
        catch (UriFormatException)
        {
            return url; // In case of a bad URL, return the original
        }
    }

    public async Task<List<BsonDocument>> ScrapeAndStoreResultsAsync(string query)
    {
        var httpClient = new HttpClient();
        var url = $"https://ahmia.fi/search/?q={query}";
        var response = await httpClient.GetStringAsync(url);

        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(response);

        var searchResults = htmlDocument.DocumentNode.SelectNodes("//li[@class='result']");
        var resultsList = new List<BsonDocument>();

        foreach (var result in searchResults)
        {
            var siteNameNode = result.SelectSingleNode(".//h4/a");
            var descriptionNode = result.SelectSingleNode(".//p");
            var onionUrlNode = result.SelectSingleNode(".//cite");

            if (siteNameNode != null && onionUrlNode != null)
            {
                string siteName = siteNameNode.InnerText.Trim();
                string description = descriptionNode != null ? descriptionNode.InnerText.Trim() : "No description available";
                string onionUrl = onionUrlNode.InnerText.Trim();
                string normalizedUrl = NormalizeUrl(onionUrl);  // Normalize the URL

                // Check if the site already exists in the database
                var existingSite = await _collection.Find(new BsonDocument("onion_url", normalizedUrl)).FirstOrDefaultAsync();

                if (existingSite == null)
                {
                    // Append the site name to the description (category field)
                    string category = $"{description} - {siteName}";

                    var document = new BsonDocument
                    {
                        { "category", category },  // Category with description + site name
                        { "flaky", true },
                        { "site_name", siteName },
                        { "onion_url", normalizedUrl }
                    };

                    // Insert into the database
                    await _collection.InsertOneAsync(document);
                    resultsList.Add(document);
                }
            }
        }

        return resultsList;
    }
}
