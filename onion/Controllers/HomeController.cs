using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using NuGet.Packaging.Licenses;

public class HomeController : Controller
{
    private readonly IMongoCollection<BsonDocument> _collection;

    // Queue to hold search requests
    private static ConcurrentQueue<string> _searchQueue = new ConcurrentQueue<string>();

    // Semaphore to limit concurrent requests
    private static SemaphoreSlim _semaphore = new SemaphoreSlim(5, 5); // Allows n request to be processed at a time

    private static ConcurrentDictionary<string, bool> _processingRequests = new ConcurrentDictionary<string, bool>(); // Thread-safe version

    public HomeController(IMongoCollection<BsonDocument> collection)
    {
        _collection = collection;
    }

    [HttpGet]
    public IActionResult SearchPage()
    {
        return View();
    }

    [HttpGet]
    public IActionResult SearchResults()
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
        _ = Task.Run(async () =>
        {
            try
            {
                await ProcessQueue(searchTerm);
            }
            catch (Exception ex)
            {
                // Log the error or handle it appropriately
                Console.WriteLine($"Error in background task: {ex.Message}");
            }
        });

        return viewResult;
    }

    // Queue processing logic
    private async Task ProcessQueue(string searchTerm)
    {
        // Add the search term to the queue
        _searchQueue.Enqueue(searchTerm);

        // Avoid multiple scrapes for the same term simultaneously
        if (_processingRequests.ContainsKey(searchTerm)) return;

        _processingRequests.TryAdd(searchTerm, true);

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
            _processingRequests.TryRemove(searchTerm, out _);
            _semaphore.Release(); // Allow the next item in the queue to process
        }
    }
}
