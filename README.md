# LumenSeek

A compact, in‑memory search engine written in C#/.NET 10. It combines a
basic web crawler, a positional inverted index, and a minimal ASP.NET Core
API to demonstrate core search‑engine concepts.

### Key features

* Crawls arbitrary URLs, parses text with HtmlAgilityPack and indexes terms
  with their positions.
* Supports TF‑IDF ranking with phrase boosts (quoted or implicit) and
  returns snippets with highlighted matches.
* Simple browser UI served from `wwwroot` plus endpoints for search,
  crawling and inspecting indexed documents.

### Tech stack

C# 12, .NET 10, ASP.NET Core minimal API, HtmlAgilityPack, concurrent
collections and compiled regex for performance.

### Highlights for a hirer

* Built a full search pipeline from crawling to ranking in a single service.
* Applied performance tuning: JIT warm‑up, posting‑list intersection,
  binary search on position arrays, and caching.
* Refactored prototype into reusable classes and added clean separation of
  UI, models and logic.

### Quick start

```bash
git clone <repo>
cd swagengine
# ensure .NET 10 SDK is installed
dotnet build    # stop any running instance first
dotnet run
```
Open `http://localhost:5000` in a browser to try the UI.

### Further work

Persistence, advanced ranking (BM25/ML), polite crawler behaviour and a
suite of tests/benchmarks are natural next steps.

This README is tailored for prospective employers to quickly grasp the
project's scope, technology choices and the skills demonstrated.
