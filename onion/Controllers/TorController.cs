using Microsoft.AspNetCore.Mvc;
using Knapcode.TorSharp;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;

namespace YourNamespace.Controllers
{
    public class HomeController : Controller
    {
        [HttpPost]
        public async Task<IActionResult> CheckWebsiteStatus([FromBody] OnionRequest request)
        {
            var isOnline = false;
            try
            {
                // Use your existing TorSharp setup
                var settings = new TorSharpSettings
                {
                    ZippedToolsDirectory = "./tor-tools",
                    ExtractedToolsDirectory = "./tor-tools/extracted",
                    PrivoxySettings = { Port = 18118 },  // Updated
                    TorSocksPort = 19050,                   // Updated
                    TorControlPort = 9051,                  // Updated
                };

                var proxy = new TorSharpProxy(settings);
                await proxy.ConfigureAndStartAsync();

                var handler = new HttpClientHandler
                {
                    Proxy = new WebProxy(new Uri("http://localhost:18118")),
                    UseProxy = true
                };

                var httpClient = new HttpClient(handler);

                // Send a request to the .onion URL
                var response = await httpClient.GetAsync(request.OnionUrl);
                isOnline = response.IsSuccessStatusCode;

                // Stop the Tor proxy
                proxy.Stop();
            }
            catch
            {
                // If there is any exception, assume the site is offline
                isOnline = false;
            }

            // Return the result as JSON
            return Json(new { isOnline });
        }
    }

    public class OnionRequest
    {
        public string OnionUrl { get; set; }
    }
}
