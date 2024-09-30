using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading.Tasks;
using System.Collections.Generic;

public class HomeController : Controller
{
    private readonly IMongoCollection<BsonDocument> _collection;

    // Inject MongoDB collection into the controller
    public HomeController(IMongoCollection<BsonDocument> collection)
    {
        _collection = collection;
    }

    [HttpGet]
    public IActionResult SearchPage()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Search(string searchTerm)
    {
        var scraper = new AhmiaScraperService(_collection);

        // Step 1: Fetch results from the database
        var existingResults = await scraper.GetResultsFromDbAsync(searchTerm);

        // Step 2: Return the existing results immediately
        ViewData["InitialResults"] = existingResults;
        var viewResult = View("SearchResults", existingResults);

        // Step 3: Trigger the scraping process for new results in the background
        _ = Task.Run(async () =>
        {
            var newResults = await scraper.ScrapeAndStoreResultsAsync(searchTerm);
            // You can log or notify the user of the new results later if needed
        });

        return viewResult; // Send the initial DB results
    }

    // Method to display all search results from the MongoDB collection
    public async Task<IActionResult> DisplaySearchResults()
    {
        var results = await _collection.Find(new BsonDocument()).ToListAsync();

        return View("SearchResults", results);
    }
}
