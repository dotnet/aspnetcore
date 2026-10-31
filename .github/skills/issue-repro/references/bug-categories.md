# Bug Categories

Identification signals and reproduction strategies by bug category.

## Contents
1. [Middleware / Pipeline Bugs](#1-middleware--pipeline-bugs)
2. [API / Model Binding Bugs](#2-api--model-binding-bugs)
3. [Blazor Bugs](#3-blazor-bugs)
4. [Auth / Security Bugs](#4-auth--security-bugs)
5. [Hosting / Startup Bugs](#5-hosting--startup-bugs)
6. [Performance / Memory Bugs](#6-performance--memory-bugs)
7. [Build / Deployment Bugs](#7-build--deployment-bugs)
8. [General Tips](#general-tips)

**Constraints applying to ALL categories:**
- Create fresh projects in `/tmp/aspnetcore/repro/{number}/` — never modify `src/`
- Source activate script from repo root before using locally-pinned SDK
- Output limits: 2KB per step success, 4KB failure
- Must test at minimum 2 ASP.NET Core versions

---

## 1. Middleware / Pipeline Bugs

**Identification signals:** Middleware not executing, wrong order effects, request not reaching endpoint, `UseXxx` not working, 404 when route should match.

### Strategy

1. **Create minimal WebAPI project:**
   ```bash
   mkdir -p /tmp/aspnetcore/repro/{number} && cd /tmp/aspnetcore/repro/{number}
   dotnet new webapi -n Repro{number} --no-openapi
   cd Repro{number}
   ```
2. **Reproduce minimal middleware configuration** from the issue.
3. **Start and make HTTP requests:**
   ```bash
   dotnet run &
   APP_PID=$!
   sleep 3  # wait for startup
   curl -sv http://localhost:5000/endpoint
   kill $APP_PID 2>/dev/null
   ```
4. **Record HTTP response** — status code, headers, body.

### Layer assignment
- `dotnet new` / package install → `setup`
- Writing Program.cs / Startup.cs → `csharp`
- `dotnet build` → `csharp`
- `dotnet run` → `hosting`
- `curl` / HTTP request → `http`

---

## 2. API / Model Binding Bugs

**Identification signals:** `null` parameter, wrong value bound, 400 Bad Request, `[FromBody]` not working, JSON serialization issues, route constraint not matching.

### Strategy

1. **Create minimal WebAPI with suspect endpoint:**
   ```csharp
   app.MapPost("/test", ([FromBody] MyModel model) =>
   {
       Console.WriteLine($"Got: {System.Text.Json.JsonSerializer.Serialize(model)}");
       return Results.Ok(model);
   });
   ```
2. **Send test request with curl:**
   ```bash
   curl -s -X POST http://localhost:5000/test \
     -H "Content-Type: application/json" \
     -d '{"name":"test","value":42}'
   ```
3. **Compare actual vs expected** — log the bound values explicitly.

### Common bindings to test
- `[FromBody]` with exact JSON
- `[FromQuery]` with URL parameters  
- `[FromRoute]` with route templates
- `[FromHeader]` with custom headers
- Complex types with nested objects

---

## 3. Blazor Bugs

**Identification signals:** Component not rendering, state not updating, JSInterop errors, lifecycle method order, event handler issues, WASM browser errors.

### Strategy for Blazor Server

1. **Create minimal Blazor Server app:**
   ```bash
   dotnet new blazorserver -n Repro{number}
   cd Repro{number}
   ```
2. **Reproduce component code** from the issue in a minimal page.
3. **Start and observe:** `dotnet run` — navigate to the page with a browser or verify console output.
4. **For UI rendering issues:** Record what is rendered vs what was expected.

### Strategy for Blazor WASM

Read [platform-blazor-wasm.md](platform-blazor-wasm.md) for browser-based reproduction steps.

### Blazor component lifecycle order

```
OnInitialized / OnInitializedAsync
OnParametersSet / OnParametersSetAsync
OnAfterRender / OnAfterRenderAsync (firstRender=true)
[parameters change] → OnParametersSet
[StateHasChanged()] → OnAfterRender (firstRender=false)
```

---

## 4. Auth / Security Bugs

**Identification signals:** 401 on valid credentials, 403 when should be authorized, cookie not set, JWT validation failing, claims not populated, policy evaluation wrong.

### Strategy

1. **Create WebAPI with auth:**
   ```bash
   dotnet new webapi -n Repro{number} --auth None
   ```
2. **Add the authentication scheme from the issue** (JWT, Cookie, etc.)
3. **Reproduce minimal auth configuration.**
4. **Test with valid and invalid credentials:**
   ```bash
   # Without token — expect 401
   curl -sv http://localhost:5000/protected
   
   # With token — expect 200
   curl -sv -H "Authorization: Bearer {token}" http://localhost:5000/protected
   ```

### Common auth pitfalls
- `UseAuthentication()` must precede `UseAuthorization()`
- JWT issuer/audience mismatch
- Cookie `SameSite` policy blocking cross-site requests
- Claims principal not set → authorization always fails

---

## 5. Hosting / Startup Bugs

**Identification signals:** `InvalidOperationException` during startup, DI registration errors, `IHostedService` not starting, configuration not loaded, `WebApplication.CreateBuilder` fails.

### Strategy

1. **Create minimal host with exact configuration from issue.**
2. **Add logging to capture startup errors:**
   ```csharp
   builder.Logging.AddConsole().SetMinimumLevel(LogLevel.Debug);
   ```
3. **Run and capture startup output:**
   ```bash
   dotnet run 2>&1 | head -50
   ```
4. **For DI issues**, enable scope validation:
   ```csharp
   builder.Host.UseDefaultServiceProvider(o =>
   {
       o.ValidateScopes = true;
       o.ValidateOnBuild = true;
   });
   ```

---

## 6. Performance / Memory Bugs

**Identification signals:** High memory usage, GC pressure, slow response times, memory leaks, request hangs, CPU spikes.

### Strategy

1. **Create load test or stress scenario:**
   ```bash
   # Make many requests in a loop
   for i in $(seq 1 100); do curl -s http://localhost:5000/endpoint; done
   ```
2. **Record process metrics:**
   ```bash
   dotnet run &
   APP_PID=$!
   sleep 3
   # Run load
   for i in $(seq 1 1000); do curl -s http://localhost:5000/ > /dev/null; done
   # Check memory
   ps -o pid,vsz,rss,comm -p $APP_PID
   kill $APP_PID
   ```
3. **For response time**, measure with `time` or curl's timing:
   ```bash
   curl -w "@-" -o /dev/null -s http://localhost:5000/ <<'EOF'
   time_total: %{time_total}s\n
   EOF
   ```

---

## 7. Build / Deployment Bugs

**Identification signals:** NuGet restore failures, publish errors, trimming issues, AOT failures, Docker build errors, `dotnet publish` fails.

### Strategy

1. **Create a minimal project from scratch** matching the reporter's `.csproj` settings.
2. **Match the exact build command** that fails:
   ```bash
   dotnet publish -r linux-x64 --self-contained -c Release
   ```
3. **Check output directory:**
   ```bash
   find bin/ -name "*.dll" | head -20
   ls bin/Release/net9.0/publish/
   ```
4. **For Docker:** Use platform file [platform-docker-linux.md](platform-docker-linux.md).

### Sub-pattern: Breaking changes in version upgrade

When the reporter upgrades .NET (e.g. 8 → 9) and gets errors:
1. **Use the OLD API** the reporter used — that's what you're reproducing
2. If `dotnet build` fails with the same errors → `conclusion: "reproduced"`, step `result: "failure"`
3. Note it's an intentional breaking change in `notes`, NOT in `conclusion`

---

## General Tips

- Always log the exact `dotnet --info` output from the temp dir (not the repo dir)
- For HTTP bugs: always include the full response (status, headers, body)
- Always test both the reporter's version AND latest stable
- Save test apps to `/tmp/aspnetcore/repro/{number}/` — clean up after
- For SignalR bugs, prefer testing with `dotnet-httprepl` or the `Microsoft.AspNetCore.SignalR.Client` NuGet package
