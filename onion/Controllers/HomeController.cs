using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Security.Claims; // For ClaimTypes
using Microsoft.AspNetCore.Http; // For HttpContext
using onion.Areas.Identity.Data; // Adjust namespace as per your project
using onion.Models; // Adjust namespace to where SearchRecord is located

public class HomeController : Controller
{
    private readonly IMongoCollection<BsonDocument> _collection;
    private readonly AuthSystemDbContext _dbContext;

    // Queue to hold search requests
    private static ConcurrentQueue<string> _searchQueue = new ConcurrentQueue<string>();

    // Semaphore to limit concurrent requests
    private static SemaphoreSlim _semaphore = new SemaphoreSlim(5, 5); // Allows 5 requests to be processed at a time

    private static ConcurrentDictionary<string, bool> _processingRequests = new ConcurrentDictionary<string, bool>(); // Thread-safe version

    public HomeController(IMongoCollection<BsonDocument> collection, AuthSystemDbContext dbContext)
    {
        _collection = collection;
        _dbContext = dbContext;
    }

    public IActionResult ForensicsTools()
    {
        var model = new ForensicsToolsViewModel();
        return View(model);
    }


    [HttpPost]
    public IActionResult ForensicsTools(ForensicsToolsViewModel model)
    {
        model.IsPost = true;

        return View(model);
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
        // Save the search term, user ID, and IP address to the database
        var userId = User.Identity.IsAuthenticated ? User.FindFirst(ClaimTypes.NameIdentifier)?.Value : null;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        // Handle possible null RemoteIpAddress 
        if (string.IsNullOrEmpty(ipAddress) && HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            ipAddress = HttpContext.Request.Headers["X-Forwarded-For"];
        }

        var searchRecord = new SearchRecord
        {
            SearchTerm = searchTerm,
            UserId = userId,
            IpAddress = ipAddress
        };

        _dbContext.SearchRecords.Add(searchRecord);
        await _dbContext.SaveChangesAsync();

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
            await _semaphore.WaitAsync(); // Ensure only a limited number of scrape operations at a time

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
