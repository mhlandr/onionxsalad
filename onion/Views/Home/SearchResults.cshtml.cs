using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace onion.Views.Home
{
    public class SearchResultModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;
        public List<SearchResult> Results { get; private set; }
        [BindProperty]
        public string SearchTerm { get; set; }
        public bool IsSearchInitiated { get; private set; }

        public SearchResultModel(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public void OnGet()
        {
            // Initial page load, no search term submitted yet
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchTerm))
            {
                ModelState.AddModelError(string.Empty, "Please enter a search term.");
                return Page();
            }

            // Initiate search via API
            IsSearchInitiated = await InitiateSearchAsync(SearchTerm);

            if (IsSearchInitiated)
            {
                // Poll for results after initiating the search
                Results = await PollForResultsAsync(SearchTerm);
            }

            return Page();
        }

        private async Task<bool> InitiateSearchAsync(string searchTerm)
        {
            var client = _clientFactory.CreateClient();
            var response = await client.PostAsync("api/searches", new StringContent(JsonSerializer.Serialize(searchTerm), Encoding.UTF8, "application/json"));

            return response.IsSuccessStatusCode;
        }

        private async Task<List<SearchResult>> PollForResultsAsync(string searchTerm)
        {
            var client = _clientFactory.CreateClient();
            List<SearchResult> results = null;

            for (int i = 0; i < 10; i++) // Retry 10 times
            {
                var response = await client.GetAsync($"api/searches/{searchTerm}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    results = JsonSerializer.Deserialize<List<SearchResult>>(jsonString);
                    if (results != null && results.Count > 0)
                        break;
                }

                await Task.Delay(3000); // Wait 3 seconds before polling again
            }

            return results ?? new List<SearchResult>();
        }
    }

    public class SearchResult
    {
        public string SiteName { get; set; }
        public string Category { get; set; }
        public string OnionUrl { get; set; }
    }
}
