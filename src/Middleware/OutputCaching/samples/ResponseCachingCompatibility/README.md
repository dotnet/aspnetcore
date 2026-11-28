# Response Caching compatibility samples

Each scenario shows one Response Caching usage, and the same application migrated to Output Caching with `UseResponseCachingSemantics()`:

```csharp
// Before
builder.Services.AddResponseCaching();
app.UseResponseCaching();

// After
builder.Services.AddOutputCache(options => options.AddBasePolicy(policy => policy.UseResponseCachingSemantics()));
app.UseOutputCache();
```

| Scenario | What it shows |
|---|---|
| [Authentication](Scenarios/Authentication.cs) | Requests with an `Authorization` header aren't cached, while cookie-authenticated users are served the cached responses. |
| [CacheControlHeaders](Scenarios/CacheControlHeaders.cs) | The endpoint sets `Cache-Control: public, max-age=3`. Clients send `no-cache` or `max-age=0`. The query string isn't part of the key. |
| [ConcurrentRequests](Scenarios/ConcurrentRequests.cs) | 5 concurrent requests while the cache is empty: the endpoint runs for each of them, since Response Caching doesn't lock. |
| [ConditionalRequests](Scenarios/ConditionalRequests.cs) | `If-None-Match` and `If-Modified-Since` are answered with a 304 from the cache. |
| [MvcResponseCacheAttribute](Scenarios/MvcResponseCacheAttribute.cs) | MVC actions with `[ResponseCache(Duration = 3)]`, `Location = Client` and `NoStore = true`. Controllers don't change. |
| [RequestCacheDirectives](Scenarios/RequestCacheDirectives.cs) | Clients send `only-if-cached`, `Pragma: no-cache`, `min-fresh`, `max-age` and `max-stale`. |
| [ResponseCompression](Scenarios/ResponseCompression.cs) | Response Compression adds `Vary: Accept-Encoding`, so one compressed response is cached per encoding. |
| [VaryByQueryKeys](Scenarios/VaryByQueryKeys.cs) | `[ResponseCache(VaryByQueryKeys = ["page"])]`. One response is cached per `page` value. The default Output Caching policy returns 500, since MVC requires `IResponseCachingFeature`. |
| [VaryHeader](Scenarios/VaryHeader.cs) | The endpoint sets `Vary: Accept-Language`. One response is cached per language. An extra column also declares the header with `SetVaryByHeader`, with the same result. |

## Running

Open `src/Middleware/Middleware.slnf` (e.g. with `src/Middleware/startvs.cmd`), set `ResponseCachingCompatibility` as the startup project and run it,
or from the command line, after running `restore.cmd`/`restore.sh` and `activate.ps1`/`activate.sh` at the repository root:

```bash
cd src/Middleware/OutputCaching/samples/ResponseCachingCompatibility
dotnet run                                              # select the scenario and the mode interactively
dotnet run -- CacheControlHeaders                       # select only the mode interactively
dotnet run -- CacheControlHeaders compare               # compares the applications side by side
dotnet run -- CacheControlHeaders responsecaching       # serves the Response Caching application on http://localhost:5000
dotnet run -- CacheControlHeaders outputcaching         # serves the migrated Output Caching application on http://localhost:5000
dotnet run -- CacheControlHeaders default               # serves the default Output Caching application on http://localhost:5000
```

The `compare` mode starts every application, sends them the scenario requests, and prints the responses.
The endpoint returns `response #N` with a counter, so a repeated number means the response was served from the cache:

```
Step                                                       ResponseCaching           OutputCaching + policy    OutputCaching default
1. First request: the endpoint runs                        response #1               response #1               response #1
2. Same request within max-age: served from cache          response #1 (Age: 0s)     response #1 (Age: 0s)     response #1 (Age: 0s)
3. Client sends 'Cache-Control: no-cache': endpoint runs   response #2               response #2               response #1 (Age: 0s)
...
```

The `OutputCaching default` column uses the default Output Caching policy, which ignores `Cache-Control` headers.

## Adding a scenario

Add a class deriving from [Scenario](Scenario.cs) in the `Scenarios` folder. It's listed automatically when the application starts.
A scenario implements:

- `CreateResponseCachingApp`: the application using Response Caching.
- `CreateOutputCachingApp`: the same application migrated to Output Caching.
- `MapEndpoints`: the endpoints, shared by all applications.
- `Steps`: the requests sent to every application in the `compare` mode. A step can send concurrent requests with `Parallel`.
- Optionally `ConfigureServices` for services shared by all applications, `ConfigurePipeline` for middleware running before the caching middleware, and `Applications` to compare more variations.
