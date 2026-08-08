# Research by Issue Type

Shared and type-specific research guidance. Read after classifying `type` in Phase 3.

## Shared Research (All Types)

Always do these regardless of type:

1. **Search for duplicates** — Use `gh search issues` across the repo:
   ```bash
   gh search issues --repo dotnet/aspnetcore --state all --limit 10 "KEYWORD1 KEYWORD2" \
     --json number,title,labels,state
   ```
2. **Check issue age and activity** — Time since last comment, whether OP responded, whether abandoned
3. **Extract all evidence** — Screenshots, code snippets, stack traces, environment details
4. **Identify .NET and ASP.NET Core version** — Check mentions, csproj references, `dotnet --info` output
5. **Check for maintainer comments** — These often contain diagnosis or workarounds already
6. **Check if recently fixed** — `git log --oneline -- src/AREA/ | head -20` for recent commits
7. **Search for workarounds** — Follow [references/workaround-search.md](workaround-search.md)

## Bug Research

**Focus:** Root cause hypothesis and workarounds.

**Where to search:**
- Stack traces → map to source (`src/Components/`, `src/Mvc/`, `src/Servers/`, etc.)
- `grep -rn "ClassName\|MethodName" src/ --include="*.cs"` → find the implementation
- `git log --oneline -- src/PATH/ | head -20` → check for recent regressions
- `gh search issues --repo dotnet/aspnetcore --state closed "error message" --limit 10` → existing reports

**Code investigation steps:**
1. Find the type/method from the stack trace or API name
2. Read the implementation — does the code match the expected behavior?
3. Check DI registration — is the service wired up correctly?
4. Check middleware ordering — is the middleware added in the right position?
5. Trace the request pipeline from `MapXxx`/`UseXxx` → handler
6. Look for recent commits touching the affected code (regression check)

**Common ASP.NET Core bug patterns:**
- Middleware order matters — check if `UseAuthentication()` comes before `UseAuthorization()`
- `IActionResult` vs value returned from minimal API — different serialization paths
- DI scope issues — `Scoped` service in `Singleton` middleware
- Cancellation token not propagated → request hangs
- `HttpContext` accessed after response completes
- `IHostedService` vs `BackgroundService` lifetime differences

**Proposal archetypes:**
1. Workaround the user can apply now (middleware reordering, config change, code pattern)
2. Fix in ASP.NET Core source (with affected files and effort estimate)
3. Alternative approach that avoids the problem entirely

## Question Research

**Focus:** Find the answer. The goal is a complete, ready-to-post response.

**Where to search:**
- `grep -rn "MethodName\|ClassUsed" src/ --include="*.cs" -l` → find correct usage in tests or samples
- `src/*/samples/` — working examples showing correct patterns
- `test/` directories — unit tests show expected API usage
- `https://learn.microsoft.com/aspnet/core/` — official documentation
- `gh search issues --repo dotnet/aspnetcore --state closed "how to" --limit 10` → prior answers

**Proposal archetypes:**
1. Direct answer with code example from source/tests
2. Pointer to documentation page or sample
3. Alternative pattern if the "obvious" approach has caveats

**Key risk:** Don't just say "check the docs" — find the specific answer.

## Feature / Enhancement Research

**Focus:** Existing alternatives and feasibility assessment.

**Where to search:**
- `grep -rn "CONCEPT" src/ --include="*.cs"` → partial solutions that already exist
- GitHub issues for similar requests (closed or open)
- GitHub PRs — has someone attempted implementation?
- `gh search prs --repo dotnet/aspnetcore "FEATURE" --limit 10`

**Proposal archetypes:**
1. Existing workaround using current APIs
2. Scope of implementation (which layers, effort estimate)
3. Link to similar/duplicate feature request

**Key risk:** Don't promise features. Frame proposals as "here's what would be involved."

## Documentation Research

**Focus:** Draft the missing documentation or identify the gap.

**Where to search:**
- `src/*/README.md` — inline documentation in source
- Test files for API usage examples: `grep -rn "ClassUsed" src/ --include="*Tests.cs"`
- Source code XML doc comments: `grep -n "/// " src/AREA/Class.cs | head -30`
- `https://learn.microsoft.com/aspnet/core/` — check what's covered

**Proposal archetypes:**
1. Draft documentation the maintainer can adapt
2. Code sample demonstrating the undocumented feature
3. Pointer to where the docs gap exists

**Key risk:** Don't generate inaccurate API docs. Verify against source code.
