using Microsoft.AspNetCore.Mvc;
using Knapcode.TorSharp;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;

public class TorController : Controller
{
    private readonly TorSharpProxy _torProxy;
    private readonly IHttpClientFactory _httpClientFactory;

    // Inject TorSharpProxy and IHttpClientFactory via constructor
    public TorController(TorSharpProxy torProxy, IHttpClientFactory httpClientFactory)
    {
        _torProxy = torProxy;
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost]
    public async Task<IActionResult> CheckWebsiteStatus([FromBody] OnionRequest request)
    {
        if (request?.OnionUrl == null)
        {
            return BadRequest("OnionUrl cannot be null.");
        }

        bool isOnline = false;

        try
        {
            // Ensure the URL has the proper scheme
            if (!request.OnionUrl.StartsWith("http://") && !request.OnionUrl.StartsWith("https://"))
            {
                request.OnionUrl = "http://" + request.OnionUrl;
            }

            // Use the injected HttpClient configured with the Tor proxy
            var httpClient = _httpClientFactory.CreateClient("TorClient");

            // Send the request to the .onion URL
            var response = await httpClient.GetAsync(request.OnionUrl);
            isOnline = response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            // Log the exception
            Console.WriteLine($"Error checking onion site: {ex.Message}");
        }

        return Json(new { isOnline });
    }
}

