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

	// Check the database for existing results asynchronously
	public async Task<List<BsonDocument>> GetResultsFromDbAsync(string query)
	{
		var filter = Builders<BsonDocument>.Filter.Regex("site_name", new BsonRegularExpression(query, "i"));
		var results = await _collection.Find(filter).ToListAsync();
		return results;
	}

	// Scrape Ahmia and store new results
	public async Task<List<BsonDocument>> ScrapeAndStoreResultsAsync(string query)
	{
		var httpClient = new HttpClient();
		var url = $"https://ahmia.fi/search/?q={query}";
		var response = await httpClient.GetStringAsync(url);

		var htmlDocument = new HtmlDocument();
		htmlDocument.LoadHtml(response);

		var searchResults = htmlDocument.DocumentNode.SelectNodes("//li[@class='result']");
		var resultsList = new List<BsonDocument>();

		// Return an empty list if no results are found
		if (searchResults == null)
		{
			return new List<BsonDocument>();
		}

		// Prepare tasks for concurrent database checks
		var tasks = new List<Task<(string onionUrl, BsonDocument existingSite)>>();

		foreach (var result in searchResults)
		{
			var siteNameNode = result.SelectSingleNode(".//h4/a");
			var descriptionNode = result.SelectSingleNode(".//p");
			var onionUrlNode = result.SelectSingleNode(".//cite");

			if (siteNameNode != null && onionUrlNode != null)
			{
				string onionUrl = onionUrlNode.InnerText.Trim();
				string normalizedUrl = NormalizeUrl(onionUrl);  // Normalize the URL

				// Perform database check in parallel
				tasks.Add(CheckSiteInDbAsync(normalizedUrl));
			}
		}

		// Await all tasks to finish (parallel processing)
		var dbCheckResults = await Task.WhenAll(tasks);

		foreach (var (onionUrl, existingSite) in dbCheckResults)
		{
			if (existingSite == null)
			{
				// Add the new document if it's not in the database
				var siteResult = searchResults.FirstOrDefault(result => NormalizeUrl(result.SelectSingleNode(".//cite").InnerText.Trim()) == onionUrl);
				if (siteResult != null)
				{
					var siteNameNode = siteResult.SelectSingleNode(".//h4/a");
					var descriptionNode = siteResult.SelectSingleNode(".//p");

					string siteName = siteNameNode.InnerText.Trim();
					string description = descriptionNode != null ? descriptionNode.InnerText.Trim() : "No description available";

					string category = $"{description} - {siteName}";

					var document = new BsonDocument
					{
						{ "category", category },
						{ "flaky", true },
						{ "site_name", siteName },
						{ "onion_url", onionUrl }
					};

					await _collection.InsertOneAsync(document);
					resultsList.Add(document);
				}
			}
		}

		return resultsList;
	}

	// Helper method to check if a site exists in the database
	private async Task<(string onionUrl, BsonDocument existingSite)> CheckSiteInDbAsync(string normalizedUrl)
	{
		var existingSite = await _collection.Find(new BsonDocument("onion_url", normalizedUrl)).FirstOrDefaultAsync();
		return (normalizedUrl, existingSite);
	}
}