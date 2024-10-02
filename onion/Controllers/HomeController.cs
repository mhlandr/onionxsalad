using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;

public class HomeController : Controller
{
    private readonly IMongoCollection<BsonDocument> _collection;

    // Queue to hold search requests
    private static ConcurrentQueue<string> _searchQueue = new ConcurrentQueue<string>();

    // Semaphore to limit concurrent requests
    private static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // Allows 1 request to be processed at a time

    private static HashSet<string> _processingRequests = new HashSet<string>(); // To avoid duplicate scrapes

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

        // Step 3: Add the search term to the queue for background processing
        _ = Task.Run(async () => await ProcessQueue(searchTerm));

        return viewResult;
    }

    // Queue processing logic
    private async Task ProcessQueue(string searchTerm)
    {
        // Add the search term to the queue
        _searchQueue.Enqueue(searchTerm);

        // Avoid multiple scrapes for the same term simultaneously
        if (_processingRequests.Contains(searchTerm)) return;

        _processingRequests.Add(searchTerm);

        try
        {
            await _semaphore.WaitAsync(); // Ensure only one scrape operation at a time

            while (_searchQueue.TryDequeue(out var termToProcess))
            {
                var scraper = new AhmiaScraperService(_collection);
                await scraper.ScrapeAndStoreResultsAsync(termToProcess);
            }
        }
        finally
        {
            _processingRequests.Remove(searchTerm);
            _semaphore.Release(); // Allow the next item in the queue to process
        }
    }
}