using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using onion.Areas.Identity.Data;
using System.Net;
using System.Net.Http;
using Knapcode.TorSharp;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Configure SQL Server for Identity
var connectionString = builder.Configuration.GetConnectionString("AuthSystemDbContexConnection")
    ?? throw new InvalidOperationException("Connection string 'AuthSystemDbContexConnection' not found.");

builder.Services.AddDbContext<AuthSystemDbContex>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDefaultIdentity<AppUser>(options =>
    options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<AuthSystemDbContex>();

// MongoDB setup
var connectionStringMongo = "mongodb://localhost:27017";
var client = new MongoClient(connectionStringMongo);
var database = client.GetDatabase("onionDB");
var collection = database.GetCollection<BsonDocument>("onion_Site");

builder.Services.AddSingleton<IMongoCollection<BsonDocument>>(collection);

// Add services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Configure Identity options
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireUppercase = false;
});

// Register TorSharpProxy
builder.Services.AddSingleton(provider =>
{
    var settings = new TorSharpSettings
    {
        ZippedToolsDirectory = "./tor-tools",
        ExtractedToolsDirectory = "./tor-tools/extracted",
        PrivoxySettings = { Port = 18118 },
        TorSocksPort = 19050,
        TorControlPort = 9051
    };

    var fetcher = new TorSharpToolFetcher(settings, new HttpClient());
    try
    {
        fetcher.FetchAsync().Wait();
        var proxy = new TorSharpProxy(settings);
        proxy.ConfigureAndStartAsync().Wait();
        return proxy;
    }
    catch (Exception ex)
    {
        var logger = provider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to start Tor proxy");
        throw;
    }
});

// Register HttpClient that uses the Tor proxy globally
builder.Services.AddHttpClient("TorClient")
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        return new HttpClientHandler
        {
            Proxy = new WebProxy(new Uri("http://localhost:18118")),
            UseProxy = true
        };
    })
    .SetHandlerLifetime(TimeSpan.FromMinutes(5))
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromMinutes(2);
    });

// Register MVC
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();


// Register ScreenshotService as a scoped service
builder.Services.AddScoped<ScreenshotService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Use CORS policy
app.UseCors("AllowSpecificOrigins");

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=SearchResults}/{id?}");

app.MapRazorPages();

app.Run();
