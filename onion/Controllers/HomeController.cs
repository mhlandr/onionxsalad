using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using onion.Models; 

namespace onion.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IMongoCollection<BsonDocument> _collection;

        // Constructor
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;

            // MongoDB connection setup (Replace with your actual MongoDB connection details)
            var client = new MongoClient("mongodb://localhost:27017"); // MongoDB connection string
            var database = client.GetDatabase("onionDB"); // Replace with your DB name
            _collection = database.GetCollection<BsonDocument>("onion_Site"); // Replace with your collection name
        }

        // Action to render the search page
        public IActionResult SearchPage()
        {
            return View();
        }

        // Action to handle the search functionality
        [HttpPost]
        public async Task<IActionResult> Search(string searchQuery)
        {
            if (string.IsNullOrEmpty(searchQuery))
            {
                ViewBag.ErrorMessage = "Search term cannot be empty.";
                return View("SearchPage");
            }

            // MongoDB query - Replace 'name' with the field you're searching for
            var filter = Builders<BsonDocument>.Filter.Regex("category", new BsonRegularExpression(searchQuery, "i"));
            var result = await _collection.Find(filter).ToListAsync();

            // Extract the relevant field values from MongoDB documents
            List<string> results = new List<string>();
            foreach (var document in result)
            {
                if (document.Contains("onion_url")) // Modify this based on your schema
                {
                    results.Add(document["onion_url"].AsString); // Replace 'name' with your desired field
                }
            }

            // Pass search results to the view
            ViewBag.Results = results;

            return View("SearchPage");
        }

        // Error handling action
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
