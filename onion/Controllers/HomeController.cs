using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using NuGet.Packaging.Licenses;



[Route("api/[controller]")]
public class SearchesController : ControllerBase
{
    private readonly IMongoCollection<BsonDocument> _collection;
    private static readonly ConcurrentQueue<string> _searchQueue = new ConcurrentQueue<string>();
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(5, 5);
    private static readonly ConcurrentDictionary<string, bool> _processingRequests = new ConcurrentDictionary<string, bool>();

    public SearchesController(IMongoCollection<BsonDocument> collection)
    {
        _collection = collection;
    }

    // Endpoint to initiate a search
    [HttpPost]
    public async Task<IActionResult> InitiateSearch([FromBody] string searchTerm)
    {
        var scraper = new AhmiaScraperService(_collection);

        // Step 1: Fetch results from the database
        var existingResults = await scraper.GetResultsFromDbAsync(searchTerm);

        // Step 2: Return existing results if available
        if (existingResults != null && existingResults.Any())
        {
            return Ok(new { message = "Results retrieved from cache", data = existingResults });
        }

        // Step 3: Add search term to the queue for background processing
        _ = Task.Run(async () => await ProcessQueue(searchTerm));

        return Accepted(new { message = "Search initiated. Processing in the background." });
    }

    // Endpoint to retrieve search results
    [HttpGet("{searchTerm}")]
    public async Task<IActionResult> GetSearchResults(string searchTerm)
    {
        var scraper = new AhmiaScraperService(_collection);
        var results = await scraper.GetResultsFromDbAsync(searchTerm);

        if (results == null || !results.Any())
            return NotFound(new { message = "No results found. Try again later." });

        return Ok(results);
    }

    // Private queue processing logic
    private async Task ProcessQueue(string searchTerm)
    {
        _searchQueue.Enqueue(searchTerm);

        // Skip if another process is already handling this term
        if (_processingRequests.ContainsKey(searchTerm)) return;

        _processingRequests.TryAdd(searchTerm, true);

        try
        {
            await _semaphore.WaitAsync();

            while (_searchQueue.TryDequeue(out var termToProcess))
            {
                var scraper = new AhmiaScraperService(_collection);
                await scraper.ScrapeAndStoreResultsAsync(termToProcess);
            }
        }
        finally
        {
            _processingRequests.TryRemove(searchTerm, out _);
            _semaphore.Release();
        }
    }
}
