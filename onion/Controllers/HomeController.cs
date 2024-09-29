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

        var results = await scraper.ScrapeAndStoreResultsAsync(searchTerm);

        return View("SearchResults", results);
    }

    // Method to display all search results from the MongoDB collection
    public async Task<IActionResult> DisplaySearchResults()
    {
        var results = await _collection.Find(new BsonDocument()).ToListAsync();

        return View("SearchResults", results);
    }
}
