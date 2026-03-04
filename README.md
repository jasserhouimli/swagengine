# LumenSeek (formerly SwagEngine)

This repository contains a simple in‑memory search engine built in C# using
.NET 10 and ASP.NET Core. The aim of the project was to explore the
fundamentals of web crawling, indexing, and query processing while
familiarising myself with the .NET web stack.

## Overview

The application behaves as follows:

- An ASP.NET minimal API provides three endpoints:
  - `GET /` serves a static single‑page interface from `wwwroot/index.html`.
  - `GET /search?q=...` accepts a query string, executes a TF‑IDF ranking
    search over an in‑memory positional inverted index, and returns JSON
    results with snippets and elapsed time in milliseconds.
  - `POST /crawl` accepts a list of seed URLs and fetches them using
    `HttpClient`/HtmlAgilityPack, extracting text and updating the index.
  - `GET /docs` returns a list of currently indexed documents (URL/text).

- The index stores term positions for each document which enables phrase
  matching and boosts phrase scores. Queries support quoted phrases and
  implicit phrase detection when the input contains multiple words.

- A lightweight front end written in plain HTML/JavaScript interacts with
  the API to perform searches, display results with highlighted terms, and
  allow new URLs to be crawled.

## Technologies used

- C# 12 / .NET 10.0
- ASP.NET Core minimal APIs
- HtmlAgilityPack for HTML parsing
- Concurrent collections for thread‑safe indexing
- Regex for tokenisation

## What I learned

Working through this project gave me hands‑on experience with several
aspects of search engine construction and web programming:

- How to build and maintain a positional inverted index, including the
  performance implications of different data structures.
- Implementing TF‑IDF ranking and normalising by document length.
- Handling phrase queries efficiently by intersecting posting lists and
  using binary search on position arrays.
- The importance of warming up the JIT and caching compiled regex for
  throughput in a long‑running server.
- Transitioning a console prototype to an ASP.NET Core web application and
  serving static files with middleware.
- Refactoring code into reusable classes, separating models and utilities,
  and keeping the web interface and back end decoupled.

## Running the project

1. Clone the repository.
2. Make sure the .NET 10 SDK is installed.
3. Restore and build using `dotnet build` (stop any running instance first).
4. Run with `dotnet run` or by launching the executable; the app listens on
   the default Kestrel port (likely `http://localhost:5000`).
5. Open a browser and navigate to `/` to use the search UI.
6. Use the crawl form or `POST /crawl` with JSON `{ "seeds": [ ... ] }` to
   add more pages to the index.

## Next steps

This is a toy project, but the codebase could be extended with:

- Persistent storage of the index (disk or a database).
- Better ranking algorithms (BM25, learning‑to‑rank).
- Polite crawling with robots.txt support and rate‑limiting.
- Automated tests and benchmarking to measure latency and accuracy.

Feel free to experiment, improve the crawler, or just learn from the code.

---

This README is intended for inclusion in a GitHub repository for others to
read and for my own reference on what the project contains and what I
picked up while building it.
