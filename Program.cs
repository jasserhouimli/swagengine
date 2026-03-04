using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var engine = new InMemorySearchEngine();

Console.WriteLine("Crawling seed pages...");
await engine.CrawlAndIndexAsync(new[]
{
    "https://example.com/",
    "https://www.iana.org/domains/reserved",

    "https://en.wikipedia.org/wiki/Search_engine",
    "https://en.wikipedia.org/wiki/Web_crawler",
    "https://en.wikipedia.org/wiki/Inverted_index",
    "https://en.wikipedia.org/wiki/PageRank",
    "https://en.wikipedia.org/wiki/Tf%E2%80%93idf",
    "https://en.wikipedia.org/wiki/Information_retrieval",
    "https://en.wikipedia.org/wiki/Natural_language_processing",
    "https://en.wikipedia.org/wiki/Machine_learning",
    "https://en.wikipedia.org/wiki/Artificial_intelligence",
    "https://en.wikipedia.org/wiki/Internet",
    "https://en.wikipedia.org/wiki/World_Wide_Web",
    "https://en.wikipedia.org/wiki/HTTP",
    "https://en.wikipedia.org/wiki/HTML",
    "https://en.wikipedia.org/wiki/Computer_science",
    "https://en.wikipedia.org/wiki/Algorithm",
    "https://x.com/home"
});

_ = engine.Search("warm up").Take(1).ToList();
Console.WriteLine($"Ready! {engine.Documents.Count} pages indexed.");

// serve static HTML/CSS from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/search", (string q) =>
{
    var sw = Stopwatch.StartNew();
    var results = engine.Search(q)
                        .Select(r => new { r.doc.Url, r.score, snippet = Utils.MakeSnippet(r.doc.Text, q) })
                        .ToArray();
    sw.Stop();
    return Results.Json(new { elapsedMs = sw.Elapsed.TotalMilliseconds, results });
});

app.MapPost("/crawl", async (CrawlRequest req) =>
{
    var seeds = req.Seeds ?? Array.Empty<string>();
    await engine.CrawlAndIndexAsync(seeds);
    return Results.Json(new { crawled = seeds.Length });
});

app.MapGet("/docs", () => engine.Documents);

app.Run();
