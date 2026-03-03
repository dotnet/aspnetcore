# Label Taxonomy

Values are **exact GitHub labels** for dotnet/aspnetcore — use exactly as listed below.

## Cardinality

| Prefix | Card. | Rule |
|--------|-------|------|
| Type labels | **1** (required) | One type per issue — pick the best fit |
| `area-` | **1** (required) | Primary component — "where in the codebase?" |
| `feature-` | **0–1** | Sub-feature within the area |
| Platform labels | **0–N** | All platforms affected (omit if cross-platform) |
| Quality labels | **0–N** | Apply if relevant |

## Valid Labels

### Type (required, single)

| Label | Use when |
|-------|----------|
| `bug` | Behavior that is not expected — code doesn't work as documented |
| `enhancement` | Improvement to existing functionality |
| `feature-request` | Something new that doesn't exist yet |
| `question` | Asks "how do I...?" — not a defect report |
| `Docs` | Documentation is missing, incorrect, or needs updating |

Trust issue content over title prefixes: `[BUG]` in title but actually asking how to do something → `question`.

### Area (required, single — pick most specific)

| Label | Covers |
|-------|--------|
| `area-blazor` | Blazor, Razor Components (use `feature-` for sub-area) |
| `area-mvc` | MVC, Actions, Controllers, Razor Views, Razor Pages, Localization, CORS, Templates |
| `area-auth` | Authentication, Authorization, OAuth, OIDC, Bearer tokens |
| `area-signalr` | SignalR hubs, clients (use `feature-client-*` for specific client) |
| `area-grpc` | gRPC wire-up, templates |
| `area-identity` | ASP.NET Core Identity (user store, sign-in manager, UI) |
| `area-dataprotection` | Data Protection APIs |
| `area-healthchecks` | Health check endpoints and middleware |
| `area-infrastructure` | Build, CI, Installers, MSBuild targets/props, Shared Framework |
| `area-commandlinetools` | dotnet-dev-certs, dotnet-user-jwts, OpenAPI CLI |
| `area-perf` | Performance infrastructure and benchmarks |

**Area disambiguation:**
- Kestrel, HTTP.sys, IIS → `area-infrastructure` if build/deploy, otherwise no dedicated area; pick the feature label
- Minimal APIs, routing, middleware → use `area-mvc` (routing lives in MVC scope) or the specific feature label
- OpenAPI / Swagger → `area-commandlinetools` (uses dotnet CLI tooling)
- HttpClientFactory → no area label; use `feature-httpclientfactory`

### Feature (optional, single — adds specificity within area)

**Blazor:**
`feature-blazor-wasm` · `feature-blazor-component-model` · `feature-blazor-jsinterop` · `feature-blazor-form-validation` · `feature-blazor-aot-compilation` · `feature-blazor-debugging` · `feature-blazor-tooling` · `feature-blazor-virtualization` · `feature-blazor-lazy-loading` · `feature-css-isolation` · `feature-hot-reload`

**Server / Hosting:**
`feature-kestrel` · `feature-httpsys` · `feature-iis` · `feature-minimal-hosting` · `feature-pipelines` · `feature-websockets`

**MVC / API:**
`feature-routing` · `feature-minimal-actions` · `feature-model-binding` · `feature-mvc-razor-views` · `feature-mvc-execution-pipeline` · `feature-mvc-application-model` · `feature-mvc-formatting` · `feature-mvc-testing` · `feature-razor-pages` · `feature-razor-sdk`

**Auth:**
`feature-oidc` · `feature-cors` · `feature-devcerts`

**SignalR:**
`feature-client-net` · `feature-client-javascript` · `feature-client-java` · `feature-client-c++`

**Other:**
`feature-openapi` · `feature-response-caching` · `feature-response-compression` · `feature-session` · `feature-static-files` · `feature-diagnostics` · `feature-localization` · `feature-caching` · `feature-httpclientfactory` · `feature-source-generators` · `feature-spa` · `feature-identity-ui` · `feature-dataprotection-redis`

### Platform (optional, multiple — omit if cross-platform)

`os/Windows` · `os/Linux` · `os/macOS` · `os/WASM`

Only add when the issue explicitly fails on a specific OS or is OS-specific.

### Quality tenets (optional, multiple)

`tenet/compatibility` · `tenet/performance` · `tenet/reliability` · `tenet/security`

### Other useful labels (do not create — reference only)

`breaking-change` · `help wanted` · `Needs: Author Feedback` · `Needs: Design` · `investigate` · `Perf` · `Security` · `blocked`

## Tips

- **Kestrel issues**: use `area-infrastructure` + `feature-kestrel`
- **Minimal API issues**: use `area-mvc` + `feature-minimal-actions`
- **Blazor WASM issues**: use `area-blazor` + `feature-blazor-wasm`
- **SignalR client issues**: use `area-signalr` + `feature-client-javascript` (or appropriate client)
- **Performance issues**: add `tenet/performance`; use `area-perf` only for infrastructure
- **Security vulnerabilities**: DO NOT file via GitHub. Direct to secure@microsoft.com.
