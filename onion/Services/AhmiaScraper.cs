using HtmlAgilityPack;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

public class AhmiaScraper
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private static readonly string _searchUrl = "https://ahmia.fi/search/?q=";

    // MongoDB settings
    private readonly IMongoCollection<BsonDocument> _collection;

    public AhmiaScraper(IMongoCollection<BsonDocument> collection)
    {
        _collection = collection;
    }

    public async Task<List<BsonDocument>> ScrapeAndStoreResults(string searchTerm)
    {
        string url = $"{_searchUrl}{Uri.EscapeDataString(searchTerm)}";
        var htmlDoc = await LoadHtmlFromUrl(url);

        if (htmlDoc == null)
            throw new Exception("Could not load the Ahmia search page.");

        var searchResults = ExtractSearchResults(htmlDoc);

        // Insert the search results into MongoDB
        await StoreResultsInDatabase(searchResults);

        return searchResults;
    }

    private async Task<HtmlDocument> LoadHtmlFromUrl(string url)
    {
        var response = await _httpClient.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            var pageContents = await response.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(pageContents);
            return doc;
        }
        return null;
    }

    private List<BsonDocument> ExtractSearchResults(HtmlDocument doc)
    {
        var results = new List<BsonDocument>();

        var resultNodes = doc.DocumentNode.SelectNodes("//li[@class='result']");
        if (resultNodes != null)
        {
            foreach (var resultNode in resultNodes)
            {
                // Extract title (site name)
                var titleNode = resultNode.SelectSingleNode(".//h4/a");
                var siteName = titleNode != null ? titleNode.InnerText.Trim() : "";

                // Extract onion link
                var onionLinkNode = resultNode.SelectSingleNode(".//h4/a[@href]");
                var onionUrl = onionLinkNode != null ? onionLinkNode.Attributes["href"].Value : "";

                // Extract description
                var descriptionNode = resultNode.SelectSingleNode(".//p");
                var description = descriptionNode != null ? descriptionNode.InnerText.Trim() : "";

                // Extract the domain or onion link
                var citeNode = resultNode.SelectSingleNode(".//cite");
                var onionUrlExtracted = citeNode != null ? citeNode.InnerText.Trim() : "";

                // Build MongoDB document
                var bsonDoc = new BsonDocument
                {
                    { "category", description },  // Put description in category
                    { "flaky", false },           // Assuming flaky status can be set to false by default
                    { "site_name", siteName },
                    { "onion_url", onionUrlExtracted },
                    { "proof_url", onionUrl }
                };

                results.Add(bsonDoc);
            }
        }

        return results;
    }

    private async Task StoreResultsInDatabase(List<BsonDocument> searchResults)
    {
        if (searchResults.Any())
        {
            await _collection.InsertManyAsync(searchResults);
        }
    }
}
