using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading.Tasks;
using System.Collections.Generic;
using MongoDB.Bson;


public class HomeController : Controller
{
    private readonly IMongoCollection<BsonDocument> _collection;

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
        var scraper = new AhmiaScraper(_collection);
        var results = await scraper.ScrapeAndStoreResults(searchTerm);

        return View("SearchResults", results);
    }

    public async Task<IActionResult> SearchResults()
    {
        var results = await _collection.Find(new BsonDocument()).ToListAsync();
        return View(results); 
    }
}
