# Platform: WebAPI / HTTP Repro

Use for minimal API, controller-based API, routing, middleware, auth, and most ASP.NET Core bugs.

## Create → Build → Run → Verify

### Create

```bash
mkdir -p /tmp/aspnetcore/repro/{number} && cd /tmp/aspnetcore/repro/{number}
dotnet new webapi -n Repro{number} --no-openapi
cd Repro{number}
```

For controller-based bugs:
```bash
dotnet new mvc -n Repro{number}
```

For Razor Pages:
```bash
dotnet new razor -n Repro{number}
```

### Customize

Replace the generated `Program.cs` with a minimal version reproducing the issue:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services from the issue
builder.Services.AddAuthentication(/* ... */);

var app = builder.Build();

// Configure middleware from the issue (order matters!)
app.UseAuthentication();
app.UseAuthorization();

// Add the endpoint that demonstrates the bug
app.MapGet("/test", () => "Hello World");

app.Run();
```

### Build

```bash
dotnet build
```

Record output and exit code.

### Run

Start in background, wait for startup, make requests, kill:

```bash
# Start server
dotnet run --no-build &
APP_PID=$!
sleep 3  # wait for startup output

# Make test request(s)
curl -sv http://localhost:5000/test

# Cleanup
kill $APP_PID 2>/dev/null
wait $APP_PID 2>/dev/null
```

For HTTPS:
```bash
curl -svk https://localhost:7000/test   # -k skips cert validation in repro
```

For POST with JSON body:
```bash
curl -sv -X POST http://localhost:5000/test \
  -H "Content-Type: application/json" \
  -d '{"key":"value"}'
```

For auth:
```bash
# Without auth — expect 401
curl -sv http://localhost:5000/secure

# With Bearer token
curl -sv -H "Authorization: Bearer {token}" http://localhost:5000/secure
```

### Verify

Check:
- HTTP status code matches (or doesn't match) expected
- Response body is correct / incorrect
- Error messages in server console output
- Exception details

## Version Testing

For each version, create a **fresh project directory**:

```bash
# Version 3A (reporter's version)
mkdir /tmp/aspnetcore/repro/{number}-v8 && cd /tmp/aspnetcore/repro/{number}-v8
dotnet new webapi --framework net8.0 -n Repro{number}v8

# Version 3B (latest)
mkdir /tmp/aspnetcore/repro/{number}-v9 && cd /tmp/aspnetcore/repro/{number}-v9
dotnet new webapi --framework net9.0 -n Repro{number}v9
```

## Changing Ports (if 5000 is in use)

```bash
ASPNETCORE_URLS=http://localhost:5100 dotnet run &
curl http://localhost:5100/test
```

## Capturing Detailed Logs

```bash
dotnet run --no-build 2>&1 | tee /tmp/aspnetcore/repro/{number}/server.log &
APP_PID=$!
sleep 3
curl -s http://localhost:5000/test
kill $APP_PID
head -50 /tmp/aspnetcore/repro/{number}/server.log
```
