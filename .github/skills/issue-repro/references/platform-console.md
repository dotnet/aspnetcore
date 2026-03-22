# Platform: Console Repro

Use for pure C# API bugs (not HTTP-specific), DI container testing, utility method bugs.

## When to Use Console

Use console when:
- The bug is in a pure C# API (e.g., data protection APIs, serialization)
- The bug doesn't require an HTTP server
- Simplest isolation possible

For most ASP.NET Core bugs, prefer [platform-webapi.md](platform-webapi.md) since the framework is HTTP-centric.

## Create → Build → Run → Verify

### Create

```bash
mkdir -p /tmp/aspnetcore/repro/{number} && cd /tmp/aspnetcore/repro/{number}
dotnet new console -n Repro{number}
cd Repro{number}
```

### Add packages from the issue

```bash
dotnet add package Microsoft.AspNetCore.DataProtection --version {version}
# or other relevant packages
```

### Write minimal reproduction in Program.cs

```csharp
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddDataProtection();
var provider = services.BuildServiceProvider();

var protector = provider.GetRequiredService<IDataProtectionProvider>()
    .CreateProtector("purpose");

// Reproduce the bug
var input = "test data";
var protected = protector.Protect(input);
var unprotected = protector.Unprotect(protected);

Console.WriteLine($"Input:       {input}");
Console.WriteLine($"Protected:   {protected}");
Console.WriteLine($"Unprotected: {unprotected}");
Console.WriteLine(input == unprotected ? "PASS" : "BUG: values differ");
```

### Build and Run

```bash
dotnet build
dotnet run
```

## Layer Assignment

| Step | Layer |
|------|-------|
| `dotnet new` / `dotnet add package` | `setup` |
| Writing code | `csharp` |
| `dotnet build` | `csharp` |
| `dotnet run` | `hosting` (even for console — it's the host) |
| Program output showing bug | `csharp` |
