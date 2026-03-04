using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

public class InMemorySearchEngine
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };
    // positional index: term -> (docId -> list of positions)
    private readonly ConcurrentDictionary<string, Dictionary<int, List<int>>> _index = new();
    private readonly List<Document> _docs = new();
    private readonly Dictionary<int, int> _docLengths = new();

    private static readonly Regex WordPattern = new(@"\w+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public InMemorySearchEngine()
    {
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("swagengine-bot/0.1");
    }

    public IReadOnlyList<Document> Documents => _docs;

    public async Task CrawlAndIndexAsync(IEnumerable<string> seeds)
    {
        int id = _docs.Count;
        foreach (var url in seeds)
        {
            try
            {
                var html = await _http.GetStringAsync(url);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var parts = new List<string>();
                var title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText;
                if (!string.IsNullOrWhiteSpace(title)) parts.Add(HtmlEntity.DeEntitize(title));

                var paragraphs = doc.DocumentNode.SelectNodes("//p");
                if (paragraphs != null)
                    parts.AddRange(paragraphs.Select(n => HtmlEntity.DeEntitize(n.InnerText)));

                var text = string.Join("\n", parts).Trim();
                if (string.IsNullOrWhiteSpace(text))
                    text = Regex.Replace(HtmlEntity.DeEntitize(doc.DocumentNode.InnerText), @"\s+", " ").Trim();

                var document = new Document(id, url, text);
                _docs.Add(document);

                var tokens = Tokenize(text);
                _docLengths[id] = tokens.Count;

                for (int pos = 0; pos < tokens.Count; pos++)
                {
                    var t = tokens[pos];
                    var posting = _index.GetOrAdd(t, _ => new Dictionary<int, List<int>>());
                    lock (posting)
                    {
                        if (!posting.TryGetValue(id, out var list))
                        {
                            list = new List<int>();
                            posting[id] = list;
                        }
                        list.Add(pos);
                    }
                }

                Console.WriteLine($"Fetched: {url} ({text.Length} chars)");
                id++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to fetch {url}: {ex.Message}");
            }
        }
    }

    public static List<string> Tokenize(string text)
    {
        var lower = text.ToLowerInvariant();
        return WordPattern.Matches(lower)
            .Select(m => m.Value)
            .Where(t => t.Length > 1)
            .ToList();
    }

    public IEnumerable<(Document doc, double score)> Search(string query)
    {
        var phraseMatches = Regex.Matches(query, "\"([^\"]+)\"");
        var phrases = phraseMatches.Cast<System.Text.RegularExpressions.Match>().Select(m => m.Groups[1].Value).ToList();

        var queryNoQuotes = Regex.Replace(query, "\"([^\"]+)\"", " ");
        var qtokens = Tokenize(queryNoQuotes);
        if (qtokens.Count > 1)
        {
            phrases.Add(string.Join(" ", qtokens));
        }

        if (!qtokens.Any() && !phrases.Any()) return Enumerable.Empty<(Document, double)>();

        int N = _docs.Count;
        var scores = new Dictionary<int, double>();

        // term-level scoring (TF-IDF)
        foreach (var term in qtokens)
        {
            if (!_index.TryGetValue(term, out var posting)) continue;
            int df = posting.Count;
            double idf = Math.Log((N + 1.0) / (df + 1.0)) + 1.0;

            foreach (var kv in posting)
            {
                int docId = kv.Key;
                int tf = kv.Value.Count;
                double tfWeight = 1 + Math.Log(tf);
                double score = tfWeight * idf;
                if (!scores.ContainsKey(docId)) scores[docId] = 0;
                scores[docId] += score;
            }
        }

        const int MaxPositionsToCheckPerDoc = 200;
        // phrase-level boosts (optimized)
        foreach (var phrase in phrases)
        {
            var pTokens = Tokenize(phrase);
            if (pTokens.Count < 1) continue;
            if (!_index.TryGetValue(pTokens[0], out var firstPosting)) continue;

            var candidates = new HashSet<int>(firstPosting.Keys);
            for (int t = 1; t < pTokens.Count && candidates.Count > 0; t++)
            {
                if (!_index.TryGetValue(pTokens[t], out var postK)) { candidates.Clear(); break; }
                candidates.IntersectWith(postK.Keys);
            }

            if (candidates.Count == 0) continue;

            foreach (var docId in candidates)
            {
                var firstPositions = firstPosting[docId];
                int checks = 0;
                bool matched = false;
                foreach (var pos in firstPositions)
                {
                    checks++;
                    if (checks > MaxPositionsToCheckPerDoc) break;
                    bool ok = true;
                    for (int k = 1; k < pTokens.Count; k++)
                    {
                        var tok = pTokens[k];
                        var postingK = _index[tok];
                        var positionsK = postingK[docId];
                        if (positionsK.BinarySearch(pos + k) < 0) { ok = false; break; }
                    }
                    if (ok) { matched = true; break; }
                }

                if (matched)
                {
                    var boost = 2.0 * pTokens.Count;
                    if (!scores.ContainsKey(docId)) scores[docId] = 0;
                    scores[docId] += boost;
                }
            }
        }

        return scores
            .Select(kv => (_docs[kv.Key], kv.Value / Math.Sqrt(_docLengths[kv.Key])))
            .OrderByDescending(x => x.Item2);
    }
}
