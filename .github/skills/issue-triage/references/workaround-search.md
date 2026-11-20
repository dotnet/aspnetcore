# Workaround Search Strategy

Systematic procedure for finding workarounds during triage.

**Goal:** Find something the reporter can use RIGHT NOW — even if a proper fix is needed later.

---

## Source Priority Order

Search in this order. Stop as soon as you find a viable workaround, but always check sources 1–3.

| Priority | Source | Why first | When to skip |
|----------|--------|-----------|-------------|
| 1 | Closed issues with maintainer responses | Often have "workaround: ..." comments | Never — always check |
| 2 | aspnetcore source tests | Show correct usage patterns | Skip if not a usage issue |
| 3 | Known patterns ([references/aspnetcore-patterns.md](aspnetcore-patterns.md)) | Curated heuristics for common traps | Never — always check |
| 4 | aspnetcore samples (`src/*/samples/`) | Working examples | Skip if deployment/config issue |
| 5 | Source code — alternative APIs | Alternative implementations visible in the class | Skip if UI/rendering issue |
| 6 | GitHub closed issues (search) | Community discoveries | After local sources exhausted |
| 7 | MS Learn / official docs | Framework documentation | Last resort for API questions |

---

## Step 1 — Extract Search Terms

From the issue body, title, and stack traces:
- **Type/class names:** `WebApplication`, `IMiddleware`, `MapGet`, `AddAuthentication`
- **Error messages:** `InvalidOperationException`, `NullReferenceException`, `404`, specific error text
- **Feature area:** Blazor, routing, auth, middleware, SignalR
- **Version:** .NET 8, ASP.NET Core 9.0.x

---

## Step 2 — Search Closed Issues

```bash
# Keyword search across closed issues
gh search issues --repo dotnet/aspnetcore --state closed --limit 15 \
  "KEYWORD1 KEYWORD2" --json number,title,state,labels

# View specific issue for maintainer comments
gh issue view {number} --repo dotnet/aspnetcore \
  --json comments --jq '.comments[] | select(.body | test("workaround|fix|resolved|try"; "i")) | {author: .author.login, body: .body[:400]}'
```

**What to look for:** OP says "I solved it by..." (high-value), maintainer suggests alternative, "closing as by-design" with explanation.

---

## Step 3 — Check Known Patterns

```bash
grep -n "KEYWORD" .github/skills/issue-triage/references/aspnetcore-patterns.md
```

---

## Step 4 — Search Source Tests

Tests show correct usage:

```bash
# Find tests for a specific class/method
grep -rln "ClassName\|MethodName" src/ --include="*Tests.cs" | head -10

# Read a test file for usage patterns
cat src/Mvc/test/Mvc.IntegrationTests/SomeTest.cs | head -80
```

---

## Step 5 — Search Source Code for Alternatives

```bash
# Public extension methods (find alternatives)
grep -n "public static.*IServiceCollection\|public static.*IApplicationBuilder\|public static.*WebApplication" \
  src/AREA/ --include="*.cs" -r | head -20

# Overloads that might have different behavior
grep -n "public.*MethodName" src/AREA/ --include="*.cs" -r
```

### Common ASP.NET Core Workarounds by Pattern

| Problem | Workaround |
|---------|------------|
| Middleware order causing auth failure | Add `UseAuthentication()` before `UseAuthorization()` |
| Route not matched | Ensure `MapControllers()` or `MapXxx()` is called, check route template syntax |
| DI scope error (`Scoped` in `Singleton`) | Use `IServiceScopeFactory` to create a new scope |
| `HttpContext` null in background thread | Capture `IHttpContextAccessor` or pass data explicitly |
| Blazor component not re-rendering | Call `StateHasChanged()` explicitly after async operations |
| CORS preflight failing | Add `app.UseCors()` BEFORE `UseAuthorization()` and routing |
| SignalR disconnection | Implement reconnection logic in client, use `RetryPolicy` |
| 404 on Razor Pages | Ensure `MapRazorPages()` is called, check page conventions |
| Slow startup in DI | Validate scopes only in Development, use `ValidateOnBuild` flag |
| JWT auth not working | Check `AddAuthentication().AddJwtBearer()` order, verify token audience/issuer |

---

## Step 6 — MS Learn / Official Docs

Use for: API how-to questions, configuration questions, deployment scenarios.

```
https://learn.microsoft.com/aspnet/core/
https://learn.microsoft.com/aspnet/core/blazor/
https://learn.microsoft.com/aspnet/core/security/
```

---

## Constructing a Workaround from Scratch

When no existing workaround is found:

1. **Identify failure boundary** — What specific call fails? DI registration, middleware invocation, request routing?
2. **Find nearest working path** — Check tests and samples for patterns near the failure
3. **Draft the workaround** — One-sentence summary, copy-pasteable code, limitations, and what a proper fix would look like
4. **Validate** — Does it avoid the failure path? Work on the reporter's platform? Reasonable burden?
