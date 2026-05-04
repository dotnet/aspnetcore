# ASP.NET Core Patterns and Common Traps

Curated heuristics for common issues in dotnet/aspnetcore. Check this during triage for instant classification and workarounds.

---

## Middleware Pipeline Traps

### Authentication / Authorization Order

**Symptom:** 401 or 403 on every request even with valid credentials.

**Trap:** `UseAuthorization()` called before `UseAuthentication()`, or before `UseRouting()`.

**Correct order:**
```csharp
app.UseRouting();
app.UseAuthentication();  // MUST come before UseAuthorization
app.UseAuthorization();
app.MapControllers();
```

**Instant workaround:** Reorder middleware.

### CORS + Auth Order

**Symptom:** CORS preflight (OPTIONS) returns 401.

**Trap:** `UseCors()` called after `UseAuthentication()`.

**Correct order:**
```csharp
app.UseCors();            // MUST come before auth
app.UseAuthentication();
app.UseAuthorization();
```

### Static Files vs Auth

**Symptom:** Static files served without auth, or auth applied to static files unintentionally.

**Pattern:** `UseStaticFiles()` before `UseAuthentication()` bypasses auth for static files (usually desired). After it, auth applies.

---

## Dependency Injection Traps

### Scoped Service in Singleton

**Symptom:** `InvalidOperationException: Cannot consume scoped service 'X' from singleton 'Y'`.

**Trap:** Injecting a `Scoped` service directly into a `Singleton`.

**Workaround:**
```csharp
public class MySingleton
{
    private readonly IServiceScopeFactory _scopeFactory;
    public MySingleton(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public void DoWork()
    {
        using var scope = _scopeFactory.CreateScope();
        var scopedService = scope.ServiceProvider.GetRequiredService<IMyService>();
        // use scopedService
    }
}
```

### IHttpContextAccessor in Background

**Symptom:** `HttpContext` is null in background threads or `IHostedService`.

**Trap:** `IHttpContextAccessor.HttpContext` is null outside of an active HTTP request.

**Workaround:** Capture needed data from `HttpContext` during the request, pass it explicitly.

### ValidateScopes Not Catching Issues

**Pattern:** Add to Development configuration:
```csharp
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = builder.Environment.IsDevelopment();
    options.ValidateOnBuild = true;
});
```

---

## Blazor Patterns

### StateHasChanged Not Called

**Symptom:** Component UI not updating after async operation.

**Trap:** After awaiting something in a component, the UI won't automatically refresh in some cases.

**Workaround:**
```csharp
await someAsyncOperation();
StateHasChanged();
```

Or use `InvokeAsync`:
```csharp
await InvokeAsync(StateHasChanged);
```

### Parameter Mutation Anti-Pattern

**Symptom:** Cascading parameter changes not reflected in child component.

**Trap:** Mutating a parameter object instead of creating a new one.

**Rule:** Parameters should be immutable or replaced, not mutated in-place.

### Blazor WASM vs Server Differences

- WASM: runs in browser, no server-side HttpContext, uses JSInterop for browser APIs
- Server: runs on server via SignalR, has full server access but UI updates go over WebSocket
- SSR (Blazor with server-side rendering): hybrid — check `RenderMode` for where code runs

### JS Interop After Render

**Symptom:** `JSException` or null reference when calling JS interop in `OnInitializedAsync`.

**Trap:** JS interop calls must happen in `OnAfterRenderAsync`, not `OnInitializedAsync`.

---

## Minimal API Patterns

### Route Not Matching

**Common causes:**
1. Route template syntax error (e.g., `{id:int}` vs `{id}`; parameter name mismatch)
2. `MapXxx()` called before or after middleware that short-circuits (e.g., `UseStaticFiles`)
3. HTTP method mismatch (POST handler but GET request)

**Debug tip:** Enable routing debugging:
```csharp
app.Use(async (ctx, next) =>
{
    Console.WriteLine($"{ctx.Request.Method} {ctx.Request.Path}");
    await next();
    Console.WriteLine($"Status: {ctx.Response.StatusCode}");
});
```

### Model Binding Failures

**Symptom:** Parameter is null or binding fails silently.

**Common causes:**
- `[FromBody]` missing on complex types for JSON binding
- Content-Type header not set to `application/json`
- Parameter name mismatch (JSON key vs C# parameter name — case-sensitive by default in .NET 9+)
- Nullable vs non-nullable parameter (null JSON value → nullable C# type required)

---

## Kestrel / Hosting Patterns

### Port Already in Use

**Symptom:** `System.IO.IOException: Failed to bind to address http://...`

**Workaround:** Change port in `appsettings.json` or via environment variable:
```
ASPNETCORE_URLS=http://localhost:5001
```

### HTTPS Developer Certificate

**Symptom:** `The SSL connection could not be established` or certificate trust errors.

**Workaround:**
```bash
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

### Large Request Body Limits

**Symptom:** 413 Payload Too Large on file uploads.

**Workaround:** Increase limit in Kestrel options or `[DisableRequestSizeLimit]` attribute.

---

## SignalR Patterns

### Hub Method Not Called

**Common causes:**
1. Hub not registered: ensure `AddSignalR()` and `MapHub<T>()` are called
2. Client not connected to correct hub URL
3. Authorization blocking — hub requires `[Authorize]`, client not authenticated

### SignalR Disconnections

**Symptom:** Client drops connection intermittently.

**Common causes:**
- Proxy/load balancer timeout (WebSocket idle timeout < hub keepalive interval)
- Server-side exception in hub method — hub automatically disconnects on unhandled exception

**Workaround for proxies:** Configure `KeepAliveInterval` and client-side reconnect:
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hub")
    .withAutomaticReconnect()
    .build();
```

---

## Auth / Identity Patterns

### JWT Token Not Validated

**Common issues:**
1. Audience mismatch — `ValidAudience` in options doesn't match `aud` claim in token
2. Issuer mismatch — `ValidIssuer` doesn't match `iss` claim
3. Clock skew — server and token issuer clocks differ by more than tolerance
4. Expired token — `nbf`/`exp` claims

### Cookie Auth Not Working

**Symptom:** User redirected to login even after signing in.

**Common causes:**
1. `DataProtection` keys not persisted — after restart, existing cookies invalid
2. Cookie domain/path mismatch
3. SameSite policy blocking cross-site requests

---

## Deployment Patterns

### App Not Finding Files After Publish

**Symptom:** `FileNotFoundException` for embedded or static resources after `dotnet publish`.

**Causes:**
1. Missing `CopyToPublishDirectory` in `.csproj`
2. `UseStaticFiles()` not configured or wrong `wwwroot` path
3. ContentRoot vs WebRoot confusion

### Docker / Linux Issues

**Symptom:** App works on Windows/macOS, fails on Linux Docker.

**Common causes:**
1. **Case sensitivity** — file paths that work on Windows/macOS fail on case-sensitive Linux
2. **Missing fonts** — text rendering requires `libfontconfig` or font packages
3. **SSL certificate** — dev certificates don't carry over to Docker; configure properly
4. **Port binding** — `localhost` binding may not expose to Docker host; use `0.0.0.0`
