using MongoDB.Bson;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// MongoDB setup 
var connectionString = "mongodb://localhost:27017";  // Use your MongoDB connection string
var client = new MongoClient(connectionString);
var database = client.GetDatabase("onionDB"); 
var collection = database.GetCollection<BsonDocument>("onion_Site"); 

builder.Services.AddSingleton<IMongoCollection<BsonDocument>>(collection);

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=SearchPage}/{id?}");

app.Run();
