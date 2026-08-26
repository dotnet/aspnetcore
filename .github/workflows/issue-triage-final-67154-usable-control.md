---
if: ${{ (github.event_name == 'workflow_dispatch' && (github.event.inputs.eval_case == 'none' || github.event.repository.fork)) || (github.event_name != 'workflow_dispatch' && !github.event.repository.fork) }}

on:
  issues:
    types: [opened]

  workflow_dispatch:
    inputs:
      issue_number:
        description: "Issue number to triage"
        required: false
        type: number
      eval_case:
        description: "Frozen replay case (forks only; all safe outputs are staged)"
        required: false
        type: choice
        default: 67154-usable-control
        options:
          - none
          - 65910-type-subtype
          - 67154-usable-control
          - 67614-startup-failure
          - 67666-failure-multi-area
          - 67766-safety-blocked
          - 67979-missing-data
          - 68331-clean-control
          - 68549-failure-multi-area
          - 68678-partial-persistence
          - 68724-automation-no-run
          - 68801-current-control
      dry_run:
        description: "If true, post analysis as a comment without applying labels"
        required: false
        type: boolean
        default: false

  roles: all

  # Force a pre_activation job to be created because pat_pool depends on it.
  # This will skip the job if there are no open issues.
  skip-if-no-match:
    query: "repo:dotnet/aspnetcore is:issue is:open"
    scope: none

description: >
  Triage newly opened issues in dotnet/aspnetcore. Classifies the area label,
  issue type, searches for potential duplicates, applies labels, and posts a
  triage summary comment on the issue. Issues that are themselves vulnerability
  reports are labelled but never commented on.

permissions:
  contents: read
  issues: read
  pull-requests: read

tools:
  bash: ["cat", "head", "tail", "grep", "wc", "jq"]
  github:
    min-integrity: none

safe-outputs:
  staged: true
  report-failure-as-issue: false
  noop:
    report-as-issue: false
  set-issue-type:
    allowed: ["Bug", "Feature", "Task", "Epic"]
    max: 1
  add-labels:
    allowed:
      - area-auth
      - area-blazor
      - area-commandlinetools
      - area-dataprotection
      - area-grpc
      - area-healthchecks
      - area-hosting
      - area-identity
      - area-infrastructure
      - area-middleware
      - area-minimal
      - area-mvc
      - area-networking
      - area-perf
      - area-routing
      - area-security
      - area-signalr
      - area-ui-rendering
      - area-unified-build
      - by-design
      - question
      - external
      - docs
      - api-proposal
      - test-failure
      - performance
    max: 3
  remove-labels:
    allowed: [needs-area-label]
    max: 1
  add-comment:
    max: 1
    target: "*"
    hide-older-comments: true

# ###############################################################
# Select a PAT from the pool and override COPILOT_GITHUB_TOKEN.
# Run agentic jobs in an isolated `copilot-pat-pool` environment.
#
# When org-level billing is available, this will be removed.
# See `shared/pat_pool.README.md` for more information.
# ###############################################################
imports:
  - uses: shared/pat_pool.md
    with:
      environment: copilot-pat-pool

environment: copilot-pat-pool

engine:
  id: copilot
  env:
    COPILOT_GITHUB_TOKEN: ${{ case(needs.pat_pool.outputs.pat_number == '0', secrets.COPILOT_PAT_0, needs.pat_pool.outputs.pat_number == '1', secrets.COPILOT_PAT_1, needs.pat_pool.outputs.pat_number == '2', secrets.COPILOT_PAT_2, needs.pat_pool.outputs.pat_number == '3', secrets.COPILOT_PAT_3, needs.pat_pool.outputs.pat_number == '4', secrets.COPILOT_PAT_4, needs.pat_pool.outputs.pat_number == '5', secrets.COPILOT_PAT_5, needs.pat_pool.outputs.pat_number == '6', secrets.COPILOT_PAT_6, needs.pat_pool.outputs.pat_number == '7', secrets.COPILOT_PAT_7, needs.pat_pool.outputs.pat_number == '8', secrets.COPILOT_PAT_8, needs.pat_pool.outputs.pat_number == '9', secrets.COPILOT_PAT_9, 'NO COPILOT PAT AVAILABLE') }}
---

# Issue Triage Agent for dotnet/aspnetcore

You are an issue-triage agent for the **dotnet/aspnetcore** repository. Your job
is to analyze a newly opened issue and perform four tasks:

1. **Area classification** - assign the correct `area-*` label
2. **Type classification** - assign an issue type (not a label) (Bug, Feature, Task, or Epic)
3. **Duplicate detection** - search for similar existing issues
4. **Triage comment** - post a single summary comment on the issue (unless the
   vulnerability gate below suppresses it)

## Issue to Triage

This is a **frozen replay evaluation**. It is staged and read-only: request the
same safe outputs you would request in production, but do not change your
classification behavior because the writes will be previewed rather than
applied.

Use this frozen point-in-time issue snapshot as the complete source of truth:

```json
{
  "schema_version": 1,
  "case_id": "67154-usable-control",
  "source": {
    "repository": "dotnet/aspnetcore",
    "issue_number": 67154,
    "issue_url": "https://github.com/dotnet/aspnetcore/issues/67154",
    "created_at": "2026-06-11T15:52:58Z",
    "snapshot_cutoff": "2026-08-26T00:00:00Z",
    "frozen_at": "2026-08-26T17:36:50Z",
    "title_and_body_edited": false,
    "body_sha256": "e334143441cf1c4fae59df0518cb90f6dacbddcd550f139ec808865576606b9d"
  },
  "issue": {
    "number": 67154,
    "title": "[API Proposal] Support modern Cache-Control directives in ResponseCacheAttribute and CacheProfile (s-maxage, stale-while-revalidate, stale-if-error) (RFC 9111, RFC 5861)",
    "body": "<!--\nDRAFT: not yet posted. Matches dotnet/aspnetcore's 30_api_proposal.md template verbatim:\nfile at https://github.com/dotnet/aspnetcore/issues/new?template=30_api_proposal.md\n(auto-labels: api-suggestion, api-proposal · type: Feature)\n\nSuggested title (verb-first, matching approved-proposal convention, see #61089, #50643, #64412):\nSupport modern Cache-Control directives in ResponseCacheAttribute and CacheProfile (s-maxage, stale-while-revalidate, stale-if-error)\n\nDelete this comment block before posting. After filing: comment on #60008 linking this issue.\n-->\n\n## Background and Motivation\n\n`[ResponseCache]` and `CacheProfile` can express `public`/`private`/`no-cache`, `max-age` and `Vary`, and that's it. That surface dates back to MVC 6 in 2015. Since then, a handful of `Cache-Control` response directives have become table stakes for anything running behind a CDN, each for a concrete reason:\n\n- `s-maxage` ([RFC 9111 §5.2.2.10](https://www.rfc-editor.org/rfc/rfc9111#section-5.2.2.10)): browsers and edges have opposite constraints. A CDN cache can be purged in milliseconds, so the edge can safely hold a response for a long time. A browser cache can't be purged at all, so you want its TTL short. With only `max-age` you're forced to pick one number for both, and it ends up being the browser's (short) one, so every edge hit expires early and the CDN is mostly wasted.\n- `stale-while-revalidate` ([RFC 5861](https://www.rfc-editor.org/rfc/rfc5861)): without it, TTL expiry means some unlucky user pays full origin latency while the cache refills (or the edge does request collapsing and a queue of users waits). With it, expiry costs nobody anything: the edge answers from the stale copy immediately and refreshes off the request path. Browsers implement it natively too.\n- `stale-if-error` (RFC 5861): turns the CDN into a last-known-good buffer. A bad deploy, an origin outage or an overloaded upstream stops being a user-facing incident for the grace window, since the edge keeps serving what it has. This is cheap, declarative resilience that otherwise needs custom VCL/workers per CDN.\n- `must-revalidate` (RFC 9111 §5.2.2.2): the opposite concern. Caches are allowed to serve stale content heuristically in some conditions (e.g.: when disconnected from the origin), and for inventory, pricing or ticket availability that's not acceptable. This directive forbids serving past expiry without revalidation.\n- `proxy-revalidate` (RFC 9111 §5.2.2.8): the same guarantee, scoped to shared caches only. You can be strict at the CDN (which serves thousands of users from one entry) while leaving the single user's browser cache relaxed.\n- `no-transform` (RFC 9111 §5.2.2.6): intermediaries and CDN features still recompress images and rewrite payloads in flight. When responses must stay byte-exact (signed content, checksummed downloads, anything a client validates against an ETag or hash), this is the directive that says hands off.\n\nCurrent support, from the origin's `Cache-Control` header with no extra configuration:\n\n- Fastly: [serving stale content](https://docs.fastly.com/en/guides/serving-stale-content) (`stale-while-revalidate`, `stale-if-error`), [s-maxage](https://docs.fastly.com/en/guides/how-caching-and-cdns-work)\n- Cloudflare: [Cache-Control directives](https://developers.cloudflare.com/cache/concepts/cache-control/) (all six)\n- CloudFront: [origin Cache-Control headers](https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/Expiration.html), [stale-while-revalidate / stale-if-error](https://aws.amazon.com/about-aws/whats-new/2023/05/amazon-cloudfront-stale-while-revalidate-stale-if-error-cache-control-directives/)\n- Browsers: [`stale-while-revalidate` on MDN](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Cache-Control#stale-while-revalidate) (Chrome, Firefox, Edge)\n\nNone of these directives can be set through the attribute or a cache profile today. The moment you need one, you have to stop using `[ResponseCache]` for that endpoint and write the header yourself in middleware.\n\nThat's what we ended up doing at SeatGeek for our ticketing APIs behind Fastly. It works, but it means the cache tiering logic lives in two places, and every endpoint that needs `s-maxage` silently opts out of the profile system. Judging by #60008, #62143, #2611 and #56769, other people keep running into versions of the same wall.\n\nThe frustrating part is that the framework already supports all of these directives: `Microsoft.Net.Http.Headers.CacheControlHeaderValue` has a `SharedMaxAge` property and an open `Extensions` collection. The only reason `[ResponseCache]` can't emit them is that `ResponseCacheFilterExecutor` builds the header with string concatenation and a three-way switch. So this is mostly a wiring exercise, not a design problem.\n\nFor comparison, other ecosystems already expose these declaratively: Spring through its `CacheControl` builder (`sMaxAge`, `staleWhileRevalidate`, `staleIfError`), Rails through `expires_in ..., stale_while_revalidate:`, Next.js/Vercel through route config. ASP.NET Core is the odd one out at the declarative layer, so this is catching up with the ecosystem rather than adding something new.\n\n(#60008 asks for RFC 5861 support in the ResponseCaching middleware. This proposal is the other half: authoring the directives declaratively in MVC.)\n\n## Proposed API\n\nProperty names follow `Microsoft.Net.Http.Headers.CacheControlHeaderValue`, which already models these directives (`SharedMaxAge` etc.), so the naming stays consistent across the framework.\n\n```diff\nnamespace Microsoft.AspNetCore.Mvc;\n\npublic class CacheProfile\n{\n+    /// <summary>\n+    /// Gets or sets the duration in seconds for which the response is cached by shared caches\n+    /// (e.g. CDNs, proxies). Sets the \"s-maxage\" directive in the \"Cache-control\" header.\n+    /// </summary>\n+    public int? SharedMaxAge { get; set; }\n+\n+    /// <summary>\n+    /// Gets or sets the duration in seconds for which a cache may serve a stale response\n+    /// while revalidating it in the background. Sets the \"stale-while-revalidate\" directive (RFC 5861).\n+    /// </summary>\n+    public int? StaleWhileRevalidate { get; set; }\n+\n+    /// <summary>\n+    /// Gets or sets the duration in seconds for which a cache may serve a stale response\n+    /// when an error occurs during revalidation. Sets the \"stale-if-error\" directive (RFC 5861).\n+    /// </summary>\n+    public int? StaleIfError { get; set; }\n+\n+    /// <summary>\n+    /// Gets or sets whether caches must revalidate stale responses before serving them.\n+    /// Sets the \"must-revalidate\" directive in the \"Cache-control\" header.\n+    /// </summary>\n+    public bool? MustRevalidate { get; set; }\n+\n+    /// <summary>\n+    /// Gets or sets whether shared caches must revalidate stale responses before serving them.\n+    /// Sets the \"proxy-revalidate\" directive in the \"Cache-control\" header.\n+    /// </summary>\n+    public bool? ProxyRevalidate { get; set; }\n+\n+    /// <summary>\n+    /// Gets or sets whether intermediaries are allowed to transform the response.\n+    /// Sets the \"no-transform\" directive in the \"Cache-control\" header.\n+    /// </summary>\n+    public bool? NoTransform { get; set; }\n}\n\npublic class ResponseCacheAttribute : Attribute, IFilterFactory, IOrderedFilter\n{\n+    /// <inheritdoc cref=\"CacheProfile.SharedMaxAge\" />\n+    public int SharedMaxAge { get; set; }\n+\n+    /// <inheritdoc cref=\"CacheProfile.StaleWhileRevalidate\" />\n+    public int StaleWhileRevalidate { get; set; }\n+\n+    /// <inheritdoc cref=\"CacheProfile.StaleIfError\" />\n+    public int StaleIfError { get; set; }\n+\n+    /// <inheritdoc cref=\"CacheProfile.MustRevalidate\" />\n+    public bool MustRevalidate { get; set; }\n+\n+    /// <inheritdoc cref=\"CacheProfile.ProxyRevalidate\" />\n+    public bool ProxyRevalidate { get; set; }\n+\n+    /// <inheritdoc cref=\"CacheProfile.NoTransform\" />\n+    public bool NoTransform { get; set; }\n}\n```\n\nThe attribute properties use the same nullable-backing-field pattern as `Duration` and `NoStore`, which is the part that makes \"not set\" and \"explicitly set to 0\" distinguishable:\n\n```csharp\nprivate int? _sharedMaxAge;\n\npublic int SharedMaxAge\n{\n    get => _sharedMaxAge ?? 0;\n    set => _sharedMaxAge = value;\n}\n```\n\nAn unset property falls through to the named profile; an explicit `SharedMaxAge = 0` overrides the profile and emits `s-maxage=0`, which is a valid value (it forces immediate revalidation by shared caches while still allowing them to store the response). Durations are `int` seconds because attribute arguments can't be `TimeSpan` (the same constraint `Duration` already lives with).\n\nOn the implementation side, `SharedMaxAge` maps to `CacheControlHeaderValue.SharedMaxAge` directly, and `stale-while-revalidate`/`stale-if-error` are appended to `CacheControlHeaderValue.Extensions` as `NameValueHeaderValue` entries (with invariant-culture integer formatting), since the typed model keeps RFC 5861 directives in its extensions collection. Directive order within the emitted header follows `CacheControlHeaderValue.ToString()` and is not guaranteed; it shouldn't be relied upon.\n\nIf the review prefers a narrower first cut: `SharedMaxAge`, `StaleWhileRevalidate` and `StaleIfError` are the motivating set and stand on their own. `MustRevalidate`/`ProxyRevalidate`/`NoTransform` are severable and can be dropped or deferred without affecting the rest.\n\nA few behavioral decisions worth calling out:\n\n1. When none of the new properties are set, the executor keeps its current string-concatenation path and the output stays byte-for-byte identical, including the missing space in `public,max-age=10`. `CacheControlHeaderValue.ToString()` puts a space after each comma, which is equivalent per RFC 9110 but would break anyone asserting on the exact header string in their tests (and plenty of test suites do). The typed composition only kicks in when at least one new directive is present.\n2. `NoStore = true` takes precedence over all the new directives, the same way it already takes precedence over `Duration`: they are ignored, nothing throws. This keeps `NoStore` usable as a kill switch on top of a profile that carries shared-cache directives.\n3. The shared-cache-only directives (`SharedMaxAge`, `ProxyRevalidate`) combined with `Location = Client` throw `InvalidOperationException` at filter execution, in the same place the missing-`Duration` check throws today. `Location = Client` emits `private`, which instructs shared caches not to store the response, so those directives would have no effect: that's always a misconfiguration, and silently emitting it would be worse than failing. Note this is deliberately scoped to the shared-cache-only directives: `StaleWhileRevalidate`/`StaleIfError` on a `private` response are valid and intentionally allowed (browsers implement `stale-while-revalidate` for their local caches, see the MDN link above), as the per-user example below shows.\n4. Small side effect of the rewrite: the current code emits a malformed `,max-age=N` when `Location` has an unrecognized value (the switch falls through to `null`). The typed path can't produce that.\n\n#### Affected components\n\nRazor Pages applies `[ResponseCache]` through its own internal `PageResponseCacheFilter`, which wraps the same executor: it picks all of this up with no additional public API (the internal filter's property mirrors get extended for consistency, covered by tests). Nothing else in the framework enumerates these directives.\n\n## Usage Examples\n\n<details open>\n<summary><b>Different TTLs for browser and edge, with staleness grace</b></summary>\n\n```csharp\n[ResponseCache(Duration = 300, SharedMaxAge = 300, StaleWhileRevalidate = 86400,\n               StaleIfError = 86400, Location = ResponseCacheLocation.Any)]\npublic IActionResult GetSiteConfiguration() => ...;\n// Cache-Control: public, max-age=300, s-maxage=300, stale-while-revalidate=86400, stale-if-error=86400\n```\n\n</details>\n\n<details>\n<summary><b>Short-lived private caching for per-user data</b> (<code>stale-while-revalidate</code>/<code>stale-if-error</code> don't require <code>s-maxage</code>)</summary>\n\n```csharp\n[ResponseCache(Duration = 5, StaleWhileRevalidate = 30, StaleIfError = 600,\n               Location = ResponseCacheLocation.Client)]\npublic IActionResult GetProfile() => ...;\n// Cache-Control: max-age=5, private, stale-while-revalidate=30, stale-if-error=600\n```\n\n</details>\n\n<details>\n<summary><b>Cache profiles</b></summary>\n\n```csharp\noptions.CacheProfiles.Add(\"EdgeCached\", new CacheProfile\n{\n    Duration = 900, SharedMaxAge = 900,\n    StaleWhileRevalidate = 86400, StaleIfError = 86400,\n    Location = ResponseCacheLocation.Any,\n});\n\n[ResponseCache(CacheProfileName = \"EdgeCached\")]\npublic IActionResult GetCaptions() => ...;\n// Cache-Control: public, max-age=900, s-maxage=900, stale-while-revalidate=86400, stale-if-error=86400\n```\n\n</details>\n\n<details>\n<summary><b>Overriding a single profile value inline</b> (same merge rules as <code>Duration</code>/<code>NoStore</code> today: attribute wins per property, the rest comes from the profile)</summary>\n\n```csharp\n[ResponseCache(CacheProfileName = \"EdgeCached\", SharedMaxAge = 60)]\npublic IActionResult GetEventList() => ...;\n// Cache-Control: public, max-age=900, s-maxage=60, stale-while-revalidate=86400, stale-if-error=86400\n```\n\n</details>\n\n<details>\n<summary><b>Controller-level attribute</b> (inherited by actions; an action-level <code>[ResponseCache]</code> replaces it entirely, existing most-effective-filter behavior, no merging)</summary>\n\n```csharp\n[ResponseCache(Duration = 60, SharedMaxAge = 120, StaleWhileRevalidate = 3600,\n               Location = ResponseCacheLocation.Any)]\npublic class CatalogController : ControllerBase\n{\n    [HttpGet(\"/catalog/list\")]\n    public string List() => ...;\n    // Cache-Control: public, max-age=60, s-maxage=120, stale-while-revalidate=3600\n\n    [HttpGet(\"/catalog/item\")]\n    [ResponseCache(Duration = 10, Location = ResponseCacheLocation.Client)]\n    public string Item() => ...;\n    // Cache-Control: private,max-age=10  (unchanged legacy output, since no new directive is set)\n}\n```\n\n</details>\n\n<details>\n<summary><b>Invalid combinations throw</b> (the same guard applies to <code>ProxyRevalidate</code> with <code>Location = Client</code>)</summary>\n\n```csharp\n[ResponseCache(Duration = 10, SharedMaxAge = 60, Location = ResponseCacheLocation.Client)]\npublic IActionResult Broken() => ...;\n// InvalidOperationException: The 'SharedMaxAge' property targets shared caches, but\n// 'Location = Client' emits \"private\", which instructs shared caches not to store the\n// response. The directive would have no effect.\n```\n\n</details>\n\n## Alternative Designs\n\n**Keep doing it in middleware.** This is the status quo and it does work. The problem is that it duplicates the location/duration logic MVC already owns, and you lose the profile system for exactly the endpoints where caching matters most. The number of issues asking for pieces of this (#60008, #62143, #2611) suggests a lot of teams have written the same middleware.\n\n**A raw extensions string instead of named properties**, something like `Extensions = \"stale-while-revalidate=86400\"`. More future-proof, but it needs input validation to avoid header injection, can't be checked for nonsensical combinations, and doesn't match how the framework models known directives elsewhere (`CacheControlHeaderValue` gives them properties too). I left it out here, but nothing in this design forecloses it: an `Extensions` property remains a door that can be opened in a follow-up for genuinely custom directives.\n\n**Include `immutable` (RFC 8246).** Asked for in #2611 and closed in 2020 for low demand, with the fair point that MVC responses are usually dynamic. It's a one-liner to add later if there's appetite. Left out of this set.\n\n**`must-understand`** is specified to travel together with `no-store`, which collides with decision 3 above, so it's omitted.\n\n**`TimeSpan` instead of `int` seconds** isn't possible on attributes. `int` also matches `Duration`.\n\n## Risks\n\n- No breaking changes: the existing code path isn't touched and its output is identical down to the byte (covered by parity tests asserting the exact strings). New behavior only happens when the new properties are used.\n- The dead combinations (`s-maxage` or `proxy-revalidate` on a `private` response) throw instead of emitting a header CDNs would ignore, so the main misuse case fails fast and loudly. `NoStore = true` keeps its existing semantics and silently wins over everything, including the new directives.\n- Performance: nothing changes on the existing path. Actions that opt into the new directives pay one `CacheControlHeaderValue` + `StringBuilder` per response, which is in the same ballpark as the strings being built anyway.\n- The ResponseCaching middleware parses `Cache-Control` through the same typed model (`stale-while-revalidate`/`stale-if-error` land in its `Extensions` collection), and whether its *serving* logic should act on them is #60008, untouched here.\n- Ships in the next major like any shared-framework API addition.\n\n---\n\n#### Live output\n\n<details>\n<summary>Captured from the working implementation: a small sample site running on <b>Kestrel</b> against locally-built bits, full response headers as curl sees them on the wire</summary>\n\n```\n$ curl -s -D - -o /dev/null http://127.0.0.1:5723/extended-public\nHTTP/1.1 200 OK\nContent-Type: text/plain; charset=utf-8\nDate: Thu, 11 Jun 2026 15:45:23 GMT\nServer: Kestrel\nCache-Control: public, max-age=300, s-maxage=300, stale-while-revalidate=86400, stale-if-error=86400\nTransfer-Encoding: chunked\n\n$ curl -s -D - -o /dev/null http://127.0.0.1:5723/legacy-parity\nHTTP/1.1 200 OK\nContent-Type: text/plain; charset=utf-8\nDate: Thu, 11 Jun 2026 15:45:23 GMT\nServer: Kestrel\nCache-Control: public,max-age=10\nTransfer-Encoding: chunked\n\n$ curl -s -D - -o /dev/null http://127.0.0.1:5723/private-burstable\nHTTP/1.1 200 OK\nContent-Type: text/plain; charset=utf-8\nDate: Thu, 11 Jun 2026 15:45:24 GMT\nServer: Kestrel\nCache-Control: max-age=5, private, stale-while-revalidate=30, stale-if-error=600\nTransfer-Encoding: chunked\n```\n\nNote the second response: an endpoint using only the pre-existing API keeps the historical output byte-for-byte (`public,max-age=10`, no space after the comma).\n\n</details>\n\n<!-- Optional closing paragraph for the issue body: -->\n\nI have a working implementation of this: unit tests (including byte-for-byte parity for the legacy path), functional tests over TestServer, Razor Pages coverage, the full Mvc.Core test suite passing, and the sample above verified over HTTP with curl for every example in this proposal. If the shape looks right, are you open to a PR? Glad to open it as soon as the API review settles.\n",
    "initial_labels": [
      "api-proposal",
      "api-suggestion",
      "area-mvc"
    ],
    "initial_type": "Feature"
  }
}
```

Do not fetch the current live issue, comments, labels, or type. Do not read
`.github/workflows/issue-triage-eval/cases.json` or any scoring output.
Looking at expected results invalidates the evaluation.

## Security Concerns Are Out of Scope

This workflow does not assess, discuss, or make recommendations about potential security implications of issues. If an issue
claims to describe a security vulnerability, do not evaluate whether the claim is valid, do not discuss the potential impact,
and do not include any security analysis in the triage report. Security assessment is handled through separate processes.

### Vulnerability Reports: Apply Labels, But Post No Comment

Before you draft anything, decide whether the issue is a **vulnerability
report**. This is the single most important decision you make, and it gates
whether you are allowed to comment at all.

**An issue is a vulnerability report if** it explicitly contains one or more of
these indicators:

- A **CVE identifier** matching the pattern `CVE-\d{4}-\d{4,}` — a 4-digit
  year followed by a 4-or-more-digit sequence number (e.g. `CVE-2020-0601`,
  `CVE-2021-44228`). The sequence number is **not** fixed-width — short
  IDs like `CVE-2020-0601` (4 digits) and long ones like `CVE-2021-44228`
  (5 digits) and `CVE-2014-0160` are all valid.
- A **specific exploit, attack vector, or proof-of-concept**: a payload
  the reporter says triggers a vulnerability ("send `${jndi:ldap://…}`",
  "I can bypass auth by setting header X to Y", "this allows arbitrary
  code execution"), step-by-step reproduction of an exploit, or magic
  strings used to demonstrate one.
- **Vulnerability-class language**: "vulnerability", "exploit",
  "remote code execution"/"RCE", "request smuggling", "header
  injection", "auth bypass", "privilege escalation", "deserialization
  attack", "SSRF", "XXE", "XSS", "CSRF" *used in the context of
  describing an attack the issue reports*. (Mere terminology in a
  feature/hardening request does NOT count — see "NOT a vulnerability
  report" below.)
- An **explicit security-fix request framed as such** — "please issue a
  security advisory", "please ship a patched release", "treat this as a
  vulnerability", "this needs to go through MSRC", "coordinated
  disclosure".

**This check is independent of whether the vulnerability is actually in
aspnetcore.** Even if you classify the issue as `external`, out-of-area,
"Not applicable", or plainly mis-filed, a vulnerability report in the issue
body **still** suppresses the comment. The reason is operational: triage
commentary on vulnerability content is unsafe regardless of repo
applicability. We do not want any public comment on a thread that reads like
a security advisory.

Concrete examples that **must** suppress the comment even if mis-filed:
- A CVE in Apache Log4j (Java) filed against `dotnet/aspnetcore`. You may
  correctly label it `external`; you **still** must not comment. Do not post
  even a polite "this isn't aspnetcore" explanation.
- A coordinated-disclosure request about a Linux kernel bug filed here.
- An "I found a vulnerability in [framework X]" report.

**An issue is NOT a vulnerability report just because** it:

- Asks for stricter parsing, hardening, RFC-compliance enforcement, or
  validation improvements without claiming an active vulnerability or
  describing an exploit.
- Touches a security-adjacent area (auth, cookies, HTTP parsing,
  antiforgery, data protection). Most issues in those areas are
  ordinary bugs and feature requests.
- Mentions security-adjacent terminology (`CR/LF`, `header`,
  `validation`, `RFC NNNN`, `harden`, `strict`, `reject`, `bypass`
  used colloquially) without describing an actual exploit.
- Compares behavior to other HTTP infrastructure (`"Squid does this"`,
  `"HaProxy added this check"`) as a feature-request rationale, as
  long as the reporter is not claiming an exploit.

**If the issue IS a vulnerability report:** still apply the area label, the
sub-type label, and the issue type exactly as you normally would (Step 7,
items 1–4), then **post no comment at all**. Skip Step 6, do **not** call
`add-comment`, and call `noop` instead:

```json
{"noop": {"message": "Triage comment suppressed: issue is a vulnerability report"}}
```

**If you are uncertain whether the issue is a vulnerability report, treat it
as one and suppress the comment.** Triage is low-stakes when skipped and
high-stakes when wrong: a missing triage comment costs a maintainer at most a
few minutes, but a triage comment on a thread that reads like a security
advisory is a public-facing mistake. The labels you applied stay in place
either way, so the issue is still discoverable.

## Do Not Classify .NET Version Release Status

Do not describe any .NET version as "preview", "RC", "stable", "released", or "unreleased". Your training data
may be outdated and you cannot reliably determine the release status of a .NET version. Simply report the version
the user mentioned (e.g., ".NET 10.0.7") without characterizing whether it is a preview or stable release.

---

## Step 1: Area Classification

Classify the issue into exactly **one** area label from the list below. Pick the
single best match based on the issue title, body, stack traces, file paths, and
API names mentioned.

### Area Labels Reference

Each area below lists key types, APIs, and concepts. Use these as strong signals
when the issue title/body mentions them.

#### `area-networking`
Kestrel, HttpSys, HTTP/2, HTTP/3, QUIC, YARP, WebSockets, HTTP abstractions, connection management.
**Code:** `src/Servers/` (Kestrel, HttpSys, IIS), `src/Http/Http/`, `src/Http/Http.Abstractions/`, `src/Http/Http.Extensions/`, `src/Http/Http.Features/`, `src/Http/Headers/`, `src/Http/WebUtilities/`, `src/Middleware/WebSockets/`, `src/Hosting/Server.Abstractions/`, `src/HttpClientFactory/`
**Namespaces:** `Microsoft.AspNetCore.Server.Kestrel.*`, `Microsoft.AspNetCore.Server.HttpSys.*`, `Microsoft.AspNetCore.Server.IIS.*`, `Microsoft.AspNetCore.Connections.*`, `Microsoft.AspNetCore.Http.*` (core abstractions), `Microsoft.AspNetCore.Http.Features.*`, `Microsoft.Net.Http.Headers.*`, `Microsoft.AspNetCore.WebUtilities.*`, `Microsoft.AspNetCore.WebSockets.*`, `Microsoft.Extensions.Http.*`
**Packages:** `Microsoft.AspNetCore.Server.Kestrel`, `Microsoft.AspNetCore.Server.Kestrel.Core`, `Microsoft.AspNetCore.Server.Kestrel.Https`, `Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets`, `Microsoft.AspNetCore.Server.Kestrel.Transport.Quic`, `Microsoft.AspNetCore.Server.HttpSys`, `Microsoft.AspNetCore.Server.IIS`, `Microsoft.AspNetCore.Connections.Abstractions`, `Microsoft.AspNetCore.Http`, `Microsoft.AspNetCore.Http.Abstractions`, `Microsoft.AspNetCore.Http.Extensions`, `Microsoft.AspNetCore.Http.Features`, `Microsoft.Net.Http.Headers`, `Microsoft.AspNetCore.WebUtilities`, `Microsoft.AspNetCore.WebSockets`
**Key types:** `KestrelServer`, `KestrelServerOptions`, `KestrelServerLimits`, `ListenOptions`, `HttpsConnectionAdapterOptions`, `Http2Limits`, `Http3Limits`, `HttpSysOptions`, `ConnectionContext`, `ConnectionHandler`, `IConnectionBuilder`, `IConnectionFactory`, `IConnectionListener`, `IConnectionListenerFactory`, `ConnectionAbortedException`, `ConnectionResetException`, `AddressInUseException`, `MinDataRate`, `PipeReader`, `PipeWriter`, `IDuplexPipe`, `IServer`
**Config:** `UseKestrel()`, `ConfigureKestrel()`, `UseHttpSys()`, `Listen()`, `ListenAnyIP()`, `ListenLocalhost()`, `UseHttps()`
**Concepts:** port binding, TLS/SSL, HTTPS, connection timeout, keep-alive, request body size limits, named pipes, Unix sockets, reverse proxy, connection middleware, transport layer, `System.IO.Pipelines`

#### `area-blazor`
Blazor, Razor Components, WebAssembly, interactive rendering modes, circuits.
**Code:** `src/Components/` (Components, Web, WebAssembly, Server, WebView, Endpoints), `src/JSInterop/`
**Namespaces:** `Microsoft.AspNetCore.Components.*`, `Microsoft.AspNetCore.Components.Web.*`, `Microsoft.AspNetCore.Components.Forms.*`, `Microsoft.AspNetCore.Components.WebAssembly.*`, `Microsoft.AspNetCore.Components.Endpoints.*`, `Microsoft.JSInterop.*`
**Packages:** `Microsoft.AspNetCore.Components`, `Microsoft.AspNetCore.Components.Web`, `Microsoft.AspNetCore.Components.Forms`, `Microsoft.AspNetCore.Components.Authorization`, `Microsoft.AspNetCore.Components.WebAssembly`, `Microsoft.AspNetCore.Components.WebAssembly.Authentication`, `Microsoft.AspNetCore.Components.WebAssembly.DevServer`, `Microsoft.AspNetCore.Components.CustomElements`, `Microsoft.AspNetCore.Components.QuickGrid`, `Microsoft.JSInterop`
**Key types:** `ComponentBase`, `LayoutComponentBase`, `DynamicComponent`, `ErrorBoundary`, `NavigationManager`, `PersistentComponentState`, `CascadingValue<T>`, `RenderMode` (`InteractiveServer`, `InteractiveWebAssembly`, `InteractiveAuto`), `EditContext`, `DataAnnotationsValidator`, `CircuitHandler`, `NavLink`, `RouteView`, `HeadOutlet`, `StreamRendering`, `IComponentRenderMode`, `RenderFragment`, `EventCallback`, `IJSRuntime`, `IJSObjectReference`, `ProtectedBrowserStorage`
**Config:** `AddRazorComponents()`, `AddInteractiveServerComponents()`, `AddInteractiveWebAssemblyComponents()`, `MapRazorComponents<T>()`
**Concepts:** `.razor` files, `@code`, render tree, JSInterop, circuit, prerendering, streaming rendering, enhanced navigation, form handling, cascading parameters, Blazor Server, Blazor WASM, Blazor Web App

#### `area-auth`
Authentication, Authorization, OAuth, OIDC, Bearer tokens, cookie auth, JWT.
**Code:** `src/Security/Authentication/`, `src/Security/Authorization/`, `src/Http/Authentication.Abstractions/`, `src/Http/Authentication.Core/`, `src/Components/Authorization/`
**Namespaces:** `Microsoft.AspNetCore.Authentication.*`, `Microsoft.AspNetCore.Authentication.Cookies.*`, `Microsoft.AspNetCore.Authentication.JwtBearer.*`, `Microsoft.AspNetCore.Authentication.OAuth.*`, `Microsoft.AspNetCore.Authentication.OpenIdConnect.*`, `Microsoft.AspNetCore.Authentication.BearerToken.*`, `Microsoft.AspNetCore.Authorization.*`
**Packages:** `Microsoft.AspNetCore.Authentication`, `Microsoft.AspNetCore.Authentication.Abstractions`, `Microsoft.AspNetCore.Authentication.Core`, `Microsoft.AspNetCore.Authentication.Cookies`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.AspNetCore.Authentication.OAuth`, `Microsoft.AspNetCore.Authentication.OpenIdConnect`, `Microsoft.AspNetCore.Authentication.BearerToken`, `Microsoft.AspNetCore.Authorization`, `Microsoft.AspNetCore.Authorization.Policy`
**Key types:** `IAuthenticationHandler`, `IAuthenticationService`, `AuthenticationMiddleware`, `AuthenticationBuilder`, `AuthenticationScheme`, `AuthenticationTicket`, `CookieAuthenticationHandler`, `CookieAuthenticationOptions`, `JwtBearerHandler`, `JwtBearerOptions`, `OAuthHandler<T>`, `OpenIdConnectHandler`, `OpenIdConnectOptions`, `IAuthorizationService`, `IAuthorizationHandler`, `IAuthorizationRequirement`, `AuthorizationPolicy`, `AuthorizationMiddleware`, `AuthorizeAttribute`, `AllowAnonymousAttribute`, `IPolicyEvaluator`, `ClaimsPrincipal`, `AuthenticateResult`
**Config:** `AddAuthentication()`, `UseAuthentication()`, `AddAuthorization()`, `UseAuthorization()`, `AddJwtBearer()`, `AddCookie()`, `AddOpenIdConnect()`, `AddOAuth()`
**Concepts:** authentication scheme, claims, bearer token, cookie auth, JWT validation, OAuth 2.0, OpenID Connect, authorization policy, `[Authorize]`, challenge, forbid, sign-in, sign-out, token validation

#### `area-identity`
ASP.NET Core Identity, user/role management, identity providers, scaffolding.
**Code:** `src/Identity/` (Core, UI, Extensions.Core, Extensions.Stores, EntityFrameworkCore)
**Namespaces:** `Microsoft.AspNetCore.Identity.*`, `Microsoft.Extensions.Identity.Core.*`, `Microsoft.Extensions.Identity.Stores.*`
**Boundary:** Identity UI scaffolding and Identity template markup belong here,
including `.razor` files under generated or project-template `Components/Account`
pages. Use `area-blazor` only when the defect is in Blazor component/runtime
behavior rather than the Identity template that consumes it.
**Packages:** `Microsoft.AspNetCore.Identity`, `Microsoft.AspNetCore.Identity.UI`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.Extensions.Identity.Core`, `Microsoft.Extensions.Identity.Stores`
**Key types:** `UserManager<TUser>`, `SignInManager<TUser>`, `RoleManager<TRole>`, `IdentityOptions`, `IdentityResult`, `IdentityError`, `IdentityUser`, `IdentityRole`, `IUserStore<T>`, `IRoleStore<T>`, `IPasswordHasher<T>`, `IUserClaimsPrincipalFactory<T>`, `ExternalLoginInfo`, `IEmailSender`, `SecurityStampValidator`, `IPasskeyHandler<T>`
**Config:** `AddIdentity<TUser,TRole>()`, `AddDefaultIdentity<TUser>()`, `MapIdentityApi<TUser>()`
**Concepts:** password hashing, two-factor authentication (2FA), external login, lockout, security stamp, email confirmation, password reset, passkey, token provider, Identity UI, Identity scaffolding, Identity API endpoints

#### `area-mvc`
MVC, Controllers, Actions, model binding, formatters, Razor Pages (page model logic).
**Code:** `src/Mvc/`, `src/Html.Abstractions/`
**Namespaces:** `Microsoft.AspNetCore.Mvc.*`, `Microsoft.AspNetCore.Mvc.Abstractions.*`, `Microsoft.AspNetCore.Mvc.ApiExplorer.*`, `Microsoft.AspNetCore.Mvc.Cors.*`, `Microsoft.AspNetCore.Mvc.DataAnnotations.*`, `Microsoft.AspNetCore.Mvc.Razor.*`, `Microsoft.AspNetCore.Mvc.RazorPages.*`, `Microsoft.AspNetCore.Mvc.TagHelpers.*`, `Microsoft.AspNetCore.Mvc.ViewFeatures.*`
**Packages:** `Microsoft.AspNetCore.Mvc`, `Microsoft.AspNetCore.Mvc.Core`, `Microsoft.AspNetCore.Mvc.Abstractions`, `Microsoft.AspNetCore.Mvc.ApiExplorer`, `Microsoft.AspNetCore.Mvc.Cors`, `Microsoft.AspNetCore.Mvc.DataAnnotations`, `Microsoft.AspNetCore.Mvc.Formatters.Json`, `Microsoft.AspNetCore.Mvc.Formatters.Xml`, `Microsoft.AspNetCore.Mvc.Localization`, `Microsoft.AspNetCore.Mvc.Razor`, `Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation`, `Microsoft.AspNetCore.Mvc.RazorPages`, `Microsoft.AspNetCore.Mvc.TagHelpers`, `Microsoft.AspNetCore.Mvc.ViewFeatures`
**Key types:** `Controller`, `ControllerBase`, `ApiControllerAttribute`, `MvcOptions`, `ApiBehaviorOptions`, `ActionResult`, `IActionResult`, `JsonResult`, `ObjectResult`, `PageModel`, `IInputFormatter`, `IOutputFormatter`, `IUrlHelper`, `IFilterMetadata`, `ModelBinderAttribute`, `BindingInfo`, `ActionContext`
**Config:** `AddMvc()`, `AddControllers()`, `AddControllersWithViews()`, `AddRazorPages()`, `MapControllers()`, `MapControllerRoute()`, `MapRazorPages()`
**Concepts:** `[ApiController]`, `[Route]`, `[HttpGet]`/`[HttpPost]`, model binding, model validation, action filters, exception filters, content negotiation, Razor Pages page model, areas, formatters

#### `area-minimal`
Minimal APIs, endpoint filters, parameter binding, request delegate generator, HTTP results.
**Code:** `src/Http/Http.Results/`, `src/OpenApi/` (OpenAPI document generation for minimal APIs)
**Namespaces:** `Microsoft.AspNetCore.Http.Result.*`, `Microsoft.AspNetCore.OpenApi.*`
**Packages:** `Microsoft.AspNetCore.Http.Results`, `Microsoft.AspNetCore.OpenApi`
**Key types:** `HttpContext`, `HttpRequest`, `HttpResponse`, `IResult`, `Results`, `TypedResults`, `IEndpointFilter`, `EndpointFilterInvocationContext`, `ProblemDetails`, `HttpValidationProblemDetails`, `IProblemDetailsService`, `IMiddleware`, `IApplicationBuilder`, `Endpoint`, `IEndpointConventionBuilder`, `BadHttpRequestException`, `IHttpContextAccessor`, `JsonOptions`
**Config:** `app.MapGet()`, `app.MapPost()`, `app.MapPut()`, `app.MapDelete()`, `app.MapPatch()`, `app.MapGroup()`, `Results.Ok()`, `Results.NotFound()`, `TypedResults.Ok()`, `AddProblemDetails()`
**Concepts:** route handler, endpoint filter, parameter binding, `[FromBody]`, `[FromQuery]`, `[FromRoute]`, `[FromHeader]`, `[FromServices]`, `[AsParameters]`, route group, request delegate, problem details

#### `area-middleware`
URL rewrite, response caching/compression, session, CORS, diagnostics, static files, rate limiting, HTTP logging, forwarded headers.
**Code:** `src/Middleware/` (CORS, Diagnostics, HttpLogging, HttpOverrides, HttpsPolicy, Localization, OutputCaching, RateLimiting, RequestDecompression, ResponseCaching, ResponseCompression, Rewrite, Session, Spa, StaticFiles, HeaderPropagation), `src/StaticAssets/`, `src/Caching/`
**Namespaces:** `Microsoft.AspNetCore.Cors.*`, `Microsoft.AspNetCore.Diagnostics.*`, `Microsoft.AspNetCore.HttpLogging.*`, `Microsoft.AspNetCore.OutputCaching.*`, `Microsoft.AspNetCore.RateLimiting.*`, `Microsoft.AspNetCore.ResponseCompression.*`, `Microsoft.AspNetCore.Rewrite.*`, `Microsoft.AspNetCore.Session.*`, `Microsoft.AspNetCore.StaticFiles.*`, `Microsoft.AspNetCore.StaticAssets.*`
**Packages:** `Microsoft.AspNetCore.Cors`, `Microsoft.AspNetCore.Diagnostics`, `Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore`, `Microsoft.AspNetCore.HttpLogging`, `Microsoft.AspNetCore.OutputCaching`, `Microsoft.AspNetCore.RateLimiting`, `Microsoft.AspNetCore.ResponseCompression`, `Microsoft.AspNetCore.Rewrite`, `Microsoft.AspNetCore.Session`, `Microsoft.AspNetCore.StaticFiles`, `Microsoft.AspNetCore.StaticAssets`, `Microsoft.AspNetCore.MiddlewareAnalysis`
**Key types:** `CorsMiddleware`, `CorsPolicy`, `DeveloperExceptionPageMiddleware`, `ExceptionHandlerMiddleware`, `IExceptionHandler`, `StatusCodePagesMiddleware`, `StaticFileMiddleware`, `SessionMiddleware`, `ResponseCompressionMiddleware`, `OutputCacheOptions`, `IOutputCacheStore`, `IRateLimiterPolicy<T>`, `HstsMiddleware`, `HttpsRedirectionMiddleware`, `RewriteMiddleware`, `ForwardedHeadersMiddleware`, `ForwardedHeadersOptions`, `ResponseCachingMiddleware`, `IHttpLoggingInterceptor`, `WebSocketOptions`
**Config:** `AddCors()` / `UseCors()`, `UseExceptionHandler()`, `UseDeveloperExceptionPage()`, `UseStaticFiles()`, `AddSession()` / `UseSession()`, `AddResponseCompression()` / `UseResponseCompression()`, `AddOutputCache()` / `UseOutputCaching()`, `AddRateLimiter()` / `UseRateLimiter()`, `UseHsts()`, `UseHttpsRedirection()`, `UseRewriter()`, `UseForwardedHeaders()`, `AddHttpLogging()` / `UseHttpLogging()`
**Concepts:** middleware pipeline, CORS policy, exception handler, static files, session state, output caching, response compression, rate limiting, HSTS, HTTPS redirect, URL rewrite, forwarded headers, X-Forwarded-For, X-Forwarded-Proto, host filtering

#### `area-signalr`
SignalR clients and servers, real-time communication, hub protocol.
**Code:** `src/SignalR/`
**Namespaces:** `Microsoft.AspNetCore.SignalR.*`, `Microsoft.AspNetCore.SignalR.Client.*`, `Microsoft.AspNetCore.Http.Connections.*`, `Microsoft.AspNetCore.SignalR.Protocols.*`
**Packages:** `Microsoft.AspNetCore.SignalR`, `Microsoft.AspNetCore.SignalR.Core`, `Microsoft.AspNetCore.SignalR.Common`, `Microsoft.AspNetCore.SignalR.Client.Core`, `Microsoft.AspNetCore.Http.Connections`, `Microsoft.AspNetCore.Http.Connections.Common`, `Microsoft.AspNetCore.Http.Connections.Client`, `Microsoft.AspNetCore.SignalR.Protocols.Json`, `Microsoft.AspNetCore.SignalR.Protocols.NewtonsoftJson`, `Microsoft.AspNetCore.SignalR.Protocols.MessagePack`
**Key types:** `Hub`, `Hub<T>`, `HubConnection`, `HubConnectionBuilder`, `HubCallerContext`, `HubConnectionContext`, `IHubContext<T>`, `IClientProxy`, `IGroupManager`, `IHubProtocol`, `HubException`, `HubOptions`, `RedisHubLifetimeManager`
**Config:** `AddSignalR()`, `MapHub<T>()`, `WithUrl()`, `.Build()`
**Concepts:** hub, hub method, real-time, WebSocket transport, Server-Sent Events, long polling, groups, streaming, MessagePack protocol, JSON protocol, reconnect, retry policy, scale-out, Redis backplane, sticky sessions

#### `area-routing`
Endpoint routing, route matching, URL generation, route constraints.
**Code:** `src/Http/Routing/`, `src/Http/Routing.Abstractions/`, `src/Http/Metadata/`
**Namespaces:** `Microsoft.AspNetCore.Routing.*`, `Microsoft.AspNetCore.Routing.Abstractions.*`
**Packages:** `Microsoft.AspNetCore.Routing`, `Microsoft.AspNetCore.Routing.Abstractions`
**Key types:** `EndpointDataSource`, `IEndpointRouteBuilder`, `LinkGenerator`, `RouteData`, `IRouteConstraint`, `IRouter`, `IParameterPolicy`, `IOutboundParameterTransformer`, `EndpointNameMetadata`
**Config:** `UseRouting()`, `UseEndpoints()`, `MapFallback()`, `RequireHost()`, `WithName()`, `AddRouting()`
**Concepts:** route template, route pattern, route constraint (`{id:int}`, `{slug:regex(...)}`), link generation, URL generation, route values, endpoint metadata, conventional vs attribute routing, catch-all routes

#### `area-dataprotection`
Data Protection APIs, key management, encryption/decryption.
**Code:** `src/DataProtection/` (DataProtection, Abstractions, Cryptography.Internal, Cryptography.KeyDerivation, Extensions, EntityFrameworkCore, StackExchangeRedis)
**Namespaces:** `Microsoft.AspNetCore.DataProtection.*`, `Microsoft.AspNetCore.Cryptography.*`
**Packages:** `Microsoft.AspNetCore.DataProtection`, `Microsoft.AspNetCore.DataProtection.Abstractions`, `Microsoft.AspNetCore.DataProtection.Extensions`, `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore`, `Microsoft.AspNetCore.DataProtection.StackExchangeRedis`, `Microsoft.AspNetCore.Cryptography.Internal`, `Microsoft.AspNetCore.Cryptography.KeyDerivation`
**Key types:** `IDataProtectionProvider`, `IDataProtector`, `ITimeLimitedDataProtector`, `DataProtectionOptions`, `IKey`, `IKeyManager`, `IXmlRepository`, `DataProtectionKey`, `KeyManagementOptions`, `IAuthenticatedEncryptor`
**Config:** `AddDataProtection()`, `PersistKeysToFileSystem()`, `PersistKeysToDbContext()`, `PersistKeysToStackExchangeRedis()`, `ProtectKeysWithCertificate()`, `SetApplicationName()`, `SetDefaultKeyLifetime()`
**Concepts:** protect/unprotect, key ring, key rotation, XML repository, purpose string, key escrow, data protector

#### `area-hosting`
Host builder, WebApplication, startup, server configuration.
**Code:** `src/Hosting/` (Hosting, Abstractions, WindowsServices), `src/DefaultBuilder/`, `src/Azure/` (AzureAppServices.HostingStartup, AzureAppServicesIntegration)
**Namespaces:** `Microsoft.AspNetCore.Hosting.*`, `Microsoft.AspNetCore.Builder.*`, `Microsoft.AspNetCore.*` (default builder)
**Packages:** `Microsoft.AspNetCore`, `Microsoft.AspNetCore.Hosting`, `Microsoft.AspNetCore.Hosting.Abstractions`, `Microsoft.AspNetCore.Hosting.Server.Abstractions`, `Microsoft.AspNetCore.TestHost`, `Microsoft.AspNetCore.Hosting.WindowsServices`
**Key types:** `WebApplication`, `WebApplicationBuilder`, `WebApplicationOptions`, `IWebHost`, `IWebHostBuilder`, `IWebHostEnvironment`, `IStartup`, `IStartupFilter`, `IHostingStartup`, `WebHostDefaults`, `StaticWebAssetsLoader`
**Config:** `WebApplication.CreateBuilder()`, `ConfigureWebHostDefaults()`, `UseStartup<T>()`, `UseUrls()`, `UseContentRoot()`
**Concepts:** `Program.cs`, `Startup.cs`, minimal hosting, Generic Host, `ASPNETCORE_URLS`, `ASPNETCORE_ENVIRONMENT`, `launchSettings.json`, hosting startup, server addresses, host configuration

#### `area-commandlinetools`
CLI tools: dotnet-dev-certs, dotnet-user-jwts, dotnet-user-secrets, OpenAPI tooling.
**Code:** `src/Tools/` (dotnet-dev-certs, dotnet-user-secrets, dotnet-user-jwts, dotnet-sql-cache, Extensions.ApiDescription.Server/Client), `src/OpenApi/Microsoft.dotnet-openapi/`, `src/ProjectTemplates/` (template infrastructure), `src/Installers/`
**Namespaces:** `Microsoft.Extensions.SecretManager.*`, `Microsoft.AspNetCore.DeveloperCertificates.*`, `Microsoft.AspNetCore.Authentication.JwtBearer.Tools.*`
**Packages:** `Microsoft.AspNetCore.DeveloperCertificates.XPlat`, `Microsoft.dotnet-openapi`, `Microsoft.Extensions.ApiDescription.Client`, `Microsoft.Extensions.ApiDescription.Server`
**Key types:** `SecretsStore`, `JwtStore`, `UserSecretsIdAttribute`
**Concepts:** `dotnet dev-certs https --trust`, `dotnet user-secrets`, `dotnet user-jwts`, `dotnet sql-cache`, `dotnet-openapi`, `secrets.json`, HTTPS dev certificate, user secrets ID
**Boundary:** Use this area for template engine, packaging, installation, and
scaffolding infrastructure. For content or assets emitted by a web template,
choose the product area that owns the generated output. Shared layouts, CSS,
JavaScript, and UI libraries used across Razor Pages, MVC, and Blazor templates
belong to `area-ui-rendering`; Blazor-specific template behavior belongs to
`area-blazor`.

#### `area-grpc`
gRPC wire-up, JSON transcoding, gRPC Swagger (main library is grpc/grpc-dotnet).
**Code:** `src/Grpc/` (JsonTranscoding, Interop)
**Namespaces:** `Microsoft.AspNetCore.Grpc.JsonTranscoding.*`, `Microsoft.AspNetCore.Grpc.Swagger.*`
**Packages:** `Microsoft.AspNetCore.Grpc.JsonTranscoding`, `Microsoft.AspNetCore.Grpc.Swagger`
**Key types:** `GrpcJsonTranscodingServiceExtensions`, `GrpcSwaggerServiceExtensions`
**Config:** `AddGrpc()`, `MapGrpcService<T>()`, `AddGrpcJsonTranscoding()`, `AddGrpcSwagger()`
**Concepts:** gRPC, protobuf, `.proto` files, gRPC-Web, JSON transcoding, gRPC Swagger, unary/streaming calls, gRPC interceptors, gRPC channels

#### `area-healthchecks`
Health check endpoints and publishers.
**Code:** `src/HealthChecks/`, `src/Middleware/HealthChecks/`
**Namespaces:** `Microsoft.Extensions.Diagnostics.HealthChecks.*`, `Microsoft.AspNetCore.Diagnostics.HealthChecks.*`
**Packages:** `Microsoft.Extensions.Diagnostics.HealthChecks`, `Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions`, `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`, `Microsoft.AspNetCore.Diagnostics.HealthChecks`
**Key types:** `IHealthCheck`, `IHealthCheckPublisher`, `HealthCheckService`, `IHealthChecksBuilder`, `HealthCheckMiddleware`, `HealthCheckOptions`, `HealthStatus` (Healthy, Degraded, Unhealthy)
**Config:** `AddHealthChecks()`, `MapHealthChecks()`, `UseHealthChecks()`
**Concepts:** liveness probe, readiness probe, health status, health check publisher, health check endpoint

#### `area-security`
Security hardening, antiforgery, cookie policy, CSRF/XSRF protection.
**Code:** `src/Antiforgery/`, `src/Security/CookiePolicy/`
**Namespaces:** `Microsoft.AspNetCore.Antiforgery.*`, `Microsoft.AspNetCore.CookiePolicy.*`
**Packages:** `Microsoft.AspNetCore.Antiforgery`, `Microsoft.AspNetCore.CookiePolicy`
**Key types:** `IAntiforgery`, `AntiforgeryOptions`, `AntiforgeryTokenSet`, `AntiforgeryValidationException`, `RequireAntiforgeryTokenAttribute`, `CookiePolicyOptions`
**Config:** `AddAntiforgery()`, `UseAntiforgery()`, `UseCookiePolicy()`
**Concepts:** antiforgery token, CSRF/XSRF, SameSite cookies, secure cookies, HTTPS enforcement, cookie policy

#### `area-ui-rendering`
MVC Views, Razor Pages (rendering/templates), TagHelpers, view compilation.
**Code:** `src/Razor/`, shared UI content/assets under `src/ProjectTemplates/Web.ProjectTemplates/`, `src/Components/Forms/`, `src/Components/QuickGrid/`, `src/Components/CustomElements/`
**Namespaces:** `Microsoft.AspNetCore.Razor.*`, `Microsoft.AspNetCore.Html.*`
**Packages:** `Microsoft.AspNetCore.Razor`, `Microsoft.AspNetCore.Razor.Runtime`, `Microsoft.AspNetCore.Html.Abstractions`
**Key types:** `ViewResult`, `PartialViewResult`, `IHtmlHelper`, `ViewDataDictionary`, `TempDataDictionary`, `ViewComponent`, `ViewComponentResult`, `RazorPagesOptions`, `AnchorTagHelper`, `FormTagHelper`, `InputTagHelper`, `CacheTagHelper`, `EnvironmentTagHelper`, `ImageTagHelper`, `GlobbingUrlBuilder`
**Concepts:** `.cshtml`, Razor syntax, `@model`, `@page`, `_ViewImports.cshtml`, `_ViewStart.cshtml`, layout, partial view, tag helper, HTML helper, view component, runtime compilation, Razor SDK, Razor Class Library (RCL), sections

#### `area-perf`
Performance regressions, benchmarks, perf infrastructure.
**Code:** (no single directory — perf benchmarks are spread across area-specific `perf/` or `benchmarks/` folders)
**Concepts:** benchmark, throughput regression, latency, RPS, memory allocation, `BenchmarkDotNet`, perf lab, crank, bombardier

#### `area-infrastructure`
Build system, CI/CD, shared framework, installers.
**Code:** `eng/`, `src/Framework/`, `src/BuildAfterTargetingPack/`, `src/Testing/`, `src/Installers/`, any file ending in `.props` or `.targets`
**Concepts:** MSBuild, `Directory.Build.props`, `Directory.Build.targets`, `eng/` scripts, Arcade SDK, source build, shared framework, targeting pack, reference assemblies, NuGet packaging, CI pipelines

#### `area-unified-build`
dotnet/dotnet unified build, source-build integration.
**Code:** `src/SiteExtensions/` (shared with infrastructure)
**Concepts:** `dotnet/dotnet` repo, unified build, source-build, VMR (Virtual Monolithic Repository)

### Disambiguation Tips

When multiple areas could match, use these priorities:
- **Pipe-level I/O** (`PipeReader`, `PipeWriter`, `IDuplexPipe`, connection handling) → `area-networking`, NOT `area-middleware`
- **Kestrel config, HTTP protocol errors, TLS/SSL** → `area-networking`
- **`Hub`, `HubConnection`, real-time** → `area-signalr` (even though SignalR uses WebSockets)
- **`ComponentBase`, `.razor`, render modes, JSInterop** → `area-blazor`
- **`.cshtml`, TagHelpers, view compilation, `ViewResult`** → `area-ui-rendering`
- **`MapGet`/`MapPost`, `Results.*`, endpoint filters** → `area-minimal`
- **`[ApiController]`, `Controller`, action filters** → `area-mvc`
- **`[Authorize]`, authentication schemes, JWT, OAuth** → `area-auth`
- **`UserManager`, `SignInManager`, Identity scaffolding/template markup** → `area-identity` (even when the template is implemented with `.razor` components)
- **Shared web-template layouts, Bootstrap/CSS/JS, and rendered UI assets** → `area-ui-rendering`, NOT `area-commandlinetools`
- **`UseCors()`, `UseStaticFiles()`, `UseSession()`, response caching** → `area-middleware`
- **Route templates, constraints, `LinkGenerator`** → `area-routing`
- **`IDataProtector`, key management, protect/unprotect** → `area-dataprotection`
- **Build failures, `eng/`, packages, CI** → `area-infrastructure`
- **`OpenApiSchemaService`, `Microsoft.AspNetCore.OpenApi.*` runtime APIs, OpenAPI document generation at request time** → `area-minimal` (the runtime OpenAPI service lives under minimal APIs)
- **`dotnet-openapi` CLI tool, `Microsoft.dotnet-openapi` package, build-time OpenAPI client/server generation** → `area-commandlinetools` (the **tool**, not the runtime service)

If you are truly unsure (confidence below ~40%), do **not** add an area label.
Explain why in the comment instead.

## Step 2: Type Classification

Classify the issue into one of these types:

| Type | When to use |
|-----------|-------------|
| `Bug` | The report clearly identifies a behavior as a bug and it can be reproduced. Something is broken or behaving unexpectedly compared to its intended design. |
| `Feature` | The report asks for a behavior that is not currently implemented. This may be a brand-new feature or an addition/enhancement to an existing feature. |
| `Task` | The issue requests bounded maintenance or implementation work that does not add product behavior, such as a documentation-only update, test/infrastructure work, or refactoring. A request to update docs or guidance should be `Task`, not `Feature`, unless it also requires new product behavior. |
| `Epic` | The issue is an umbrella or tracking item that intentionally coordinates multiple independently deliverable issues. Do not use this for a single feature request. |

Do not choose `Task` merely because the fix is small or mechanical. If the
current shipped product or generated output demonstrably violates an expected
baseline, classify it as `Bug` even when the fix is a dependency/version bump.
Reserve `Task` for planned cleanup or maintenance where current behavior is not
itself the reported defect.

## Step 3: Additional Labels

Classify the issue using one of these labels, if applicable:

| `by-design` | The report describes a behavior that doesn't match the reporter's expectations, but the behavior is actually the intended design. |
| `question` | The report describes expected behavior, asks for clarification on how to use the product, or is a general "How do I...?" question. Mark as answered when a response is provided. |
| `external` | The report is not related to an area that the aspnetcore team owns directly. The issue should be moved to the appropriate repo or the customer should be asked to file through the appropriate channels (typically VS Feedback). |
| `docs` | Documentation issue, missing/incorrect docs. |
| `api-proposal` | Formal API addition/change proposal. |
| `test-failure` | CI/test infrastructure failure report. |
| `performance` | Performance regression or optimization request. |

If the requested deliverable is only a documentation or guidance update, apply
`docs`. If the issue explicitly establishes that the root cause and required
fix belong to an external tool or repository, apply `external` even when the
symptom appears in an ASP.NET Core area.

Apply the single best label (if applicable). If the issue template already indicates the type
(e.g., filed via the bug report template), trust that signal but verify it matches
the actual content — reporters sometimes pick the wrong template.

## Step 4: Regression Detection

If the issue is classified as a `bug`, check whether it describes a **regression** —
a behavior that previously worked in an older version but is now broken in a newer one.

**Look for these signals in the issue body:**
- Explicit mentions of a version where it **used to work** (e.g., ".NET 8", "ASP.NET Core 7.0.x", "worked in preview 3")
- Explicit mentions of a version where it **stopped working** (e.g., "after upgrading to .NET 9", "broken since 9.0.1")
- Phrases like "regression", "used to work", "broke after update", "worked before", "behavior changed"
- References to specific release notes, preview builds, or SDK versions

**If regression information is present**, include a **Regression** section in the
triage summary with:
- **Previously working version:** the version where the behavior was correct (if stated)
- **Broken since:** the version where the regression appeared (if stated)
- A brief note on the behavior change (what worked vs. what no longer works)

If the author mentions a regression but does not specify exact versions, note what
is known and flag that more information may be needed from the author.

If there is no indication of a regression, omit this section from the summary.

## Step 5: Duplicate Detection

Search for potential duplicates among recent open issues using the GitHub MCP
Server tools:

- Use the `search_issues` tool from the **github** MCP server to find issues
  matching relevant keywords. Filter by repository and open state.

Extract 2-4 key technical terms from the issue (e.g., API names, error messages,
component names) and search for them. Try **2 different searches** with
different keyword combinations to cast a wider net.

**Evaluation criteria:**
- Same component AND same symptom/request → likely duplicate
- Same component but different problem → not a duplicate
- Similar error message but different context → mention but don't call it a duplicate

Only flag an issue as a potential duplicate if you have **high confidence** that
it describes the same problem or feature request. When in doubt, list it as
"related" rather than "duplicate".

## Step 6: Draft the Triage Comment

Compose a single triage comment summarizing your findings. Use **exactly** this
structure — no additional sections beyond what is listed below:

```markdown
### Triage Summary

**Area:** `area-xyz` (brief reason)
**Type:** `Bug` | `Feature` | `Task` | `Epic` (brief reason)

#### Regression Info
- **Previously working version:** .NET x.y / ASP.NET Core x.y
- **Broken since:** .NET x.y / ASP.NET Core x.y
- Brief description of the behavior change
- _(Omit this entire section if the issue is not a regression)_

#### Potential Duplicates
- #123 - Title (similarity: high/medium)
- _(Always include this section. If you found no candidate duplicates, write a single bullet `- _None found_` and omit any per-issue bullets.)_

#### Notes
- _(Optional, additive-only. See "What belongs in Notes" below. Omit the entire section if you have nothing of this kind to add.)_
```

### Comment-Wide Content Rules

These rules apply to **every part** of the comment — the Area/Type lines, the
Regression Info section, the Potential Duplicates section, and Notes alike.

1. **No constructed security analysis.** Do not add security framing,
   hardening rationale, vulnerability-impact analysis, or
   RFC-compliance-as-a-security-argument that the issue itself does not
   make — e.g. *"this could lead to request smuggling"*, *"recommend
   treating as a security fix"*, *"aligns with security best practices"*.
   You **may** factually restate the issue's own framing in the Area/Type
   parentheticals — echoing the reporter's own words (e.g. echoing a title
   like *"Harden CR/LF handling…"*) is reporting, not construction.

2. **No third-party infrastructure comparisons.** Do not cite Squid,
   HaProxy, NGINX, or other HTTP infrastructure as a hardening or
   correctness argument — not even if the issue body mentions them. They
   do not belong in a triage classification.

3. **No labels in the comment body.** Do not add a `#### Labels Applied`
   section, do not list the labels you applied, and do not recommend
   additional ones (*"Recommend also labeling with `security`"*). The
   applied labels are visible in the issue's label sidebar, which is the
   source of truth.

4. **No .NET version-status claims.** Do not call a version "preview",
   "RC", "stable", "released", or "unreleased". State the version number
   the reporter gave (e.g. ".NET 10.0.7") and let the maintainer judge
   release status.

5. **No editorializing about the issue's validity.** Do not argue whether
   the issue is *"valid"*, *"actionable"*, or *"worth fixing"*, do not
   praise or criticize the report (*"This is a reasonable request,"* *"The
   proposal is well-documented,"* *"The author correctly identifies the
   root cause"*), and do not assign blame to the reporter. Maintainers do
   not need an LLM's opinion on issue quality.

6. **No speculation.** Every claim must be verifiable from the issue body,
   the repository, or a tool call you actually made. *"The error message
   suggests X is missing"* is speculation; *"git blame on file:line shows
   the check was removed in PR #NNNN"* is evidence. If you cannot verify
   it, leave it out.

7. **Only verified duplicate citations.** Before citing a `#NNN` under
   `#### Potential Duplicates`, verify with the `issue_read` MCP tool that
   it exists and is plausibly related (same component **and** same
   symptom/request). Drop any citation you cannot verify or that is
   clearly unrelated — different area or different problem. If nothing
   survives, write the single bullet `- _None found_`.

8. **No extra sections and no meta-commentary.** Use exactly the headings
   from the template above. No verdict lines, no footers, no commentary
   about the triage process itself.

### Section-Shape Rules

- If a section would have no content after applying the rules above,
  **omit its heading entirely**. A bare `#### Notes` or `#### Regression
  Info` heading with nothing under it is worse than no section at all.
- **Exception:** `#### Potential Duplicates` always keeps its heading. If
  you have no verified duplicates, keep the heading and write the single
  bullet `- _None found_`.
- Never leave dangling punctuation or half-sentences behind. If dropping a
  phrase would leave a fragment (e.g. `**Type:** Bug (, request smuggling
  vector)`), drop the whole parenthetical or the whole sentence instead.

### What belongs in `#### Notes`

Notes is an **additive** section. Everything in it must be (a) new
information not already stated in the issue body and (b) verifiable,
not speculative. Acceptable kinds of bullets, in priority order:

1. **Concrete code pointers** for the maintainer — file + symbol where
   the relevant logic lives, e.g. *"Likely in
   `src/Http/Routing/src/Matching/DfaMatcherBuilder.cs:Build()`"*. Only
   include a pointer you can actually justify from the issue
   description and the repo structure. Do not invent line numbers.

2. **Deterministic regression evidence** — *only if the reporter did
   NOT already state versions*. If you can verify via `git blame` /
   commit history / PR references the precise commit or PR that
   introduced the behavior, name it: *"Behavior introduced by PR
   #NNNN merged in .NET 10.0.5"*. Do **not** guess (*"may have been
   introduced in 10.0.4"* is a guess — drop it). If you cannot verify
   deterministically, omit this bullet.

3. **Reproduction requests** — flag when the reporter omitted critical
   information needed to act on the issue. Be specific: list exactly
   what is missing. E.g. *"Missing: runtime version, full stack trace,
   minimal repro"*. Do not list every theoretically-useful field —
   only what is actually required to act.

4. **Verified cross-references** — issue is already closed by a
   maintainer, is a sub-issue of #NNN, is a verified duplicate of an
   open #NNN. You must verify the cited issue's state via the `issue_read`
   MCP tool before including it.

### What does NOT belong in `#### Notes`

Every comment-wide content rule above applies inside Notes too. In addition,
Notes specifically must not contain:

- **Rephrasing the issue body.** If the reporter said *"X throws Y on
  Z"*, do not write *"The issue reports that X throws Y on Z"*. That
  is noise. Notes is for new information only. Compare every bullet
  against the issue body you read and drop anything that merely
  restates it.
- **"This might be related to…" hypotheses.** Speculation is already
  banned comment-wide, but in Notes it is the most common failure
  mode, so re-check every bullet for hedging language — *"may be
  related to,"* *"the error suggests,"* *"likely caused by,"*
  *"appears to be,"* *"this suggests."* If a bullet needs a hedge, it
  is not verifiable — drop it.

If after applying these rules you have nothing left to say, **omit the
`#### Notes` section entirely**. An empty Notes section is worse than
no Notes section.

## Step 7: Apply Labels, Type, and Post the Comment

Order of operations matters. Do these in this exact order:

1. **Decide the labels and issue type** you will apply, based on Steps 1–5,
   then compare them with the issue's current labels and type. For a frozen
   replay, `issue.initial_labels` and `issue.initial_type` are the current
   state.

   If the issue already has the chosen area, supported sub-type (if any), and
   issue type; does not need `needs-area-label` removed; and has no newly
   verified duplicate that the reporter did not already cite, call `noop` and
   stop. A related issue or duplicate candidate already linked in the title or
   body is not new triage information and does not prevent this no-op.

2. **Apply the area label** and (if applicable from Step 3) one **additional
   sub-type label** using the `add-labels` safe output. The `add-labels`
   allowed list includes the area labels and the sub-type labels
   (`by-design`, `question`, `external`, `docs`, `api-proposal`,
   `test-failure`, `performance`). It does **not** include issue types
   (`Bug`, `Feature`, `Task`, or `Epic`); apply those via `set-issue-type` in
   step 3 below. Include `item_number` with the number of the issue being
   triaged. For a frozen replay, use the issue number in the snapshot.
   Otherwise, use `${{ github.event.issue.number }}` for `issues.opened` runs
   or `${{ github.event.inputs.issue_number }}` for `workflow_dispatch` runs.
   Do not request labels the issue already has. Skip `add-labels` if no chosen
   labels are missing.

3. **Apply the issue type** using `set-issue-type` with one of `Bug`,
   `Feature`, `Task`, or `Epic` based on your Step 2 classification. Include
   `issue_number` with the same explicit issue number used for `add-labels`.
   Call `set-issue-type` at most once, and only if the issue's current type is
   missing or differs from the chosen type.

4. If the issue currently has `needs-area-label` and you assigned an area,
   **remove `needs-area-label`** using `remove-labels`. Include `item_number`
   with the same explicit issue number used for `add-labels`.

5. **Apply the vulnerability gate.** If the issue is a vulnerability report
   per "Vulnerability Reports: Apply Labels, But Post No Comment" above,
   stop here: call `noop` and do **not** call `add-comment`. The labels and
   issue type you applied in steps 2–4 stay in place. Otherwise continue.

6. **Draft the comment per Step 6.** The applied labels and issue type
   are visible in the issue's label sidebar; do not list them inside
   the comment body.

7. **Post the comment with the `add-comment` safe output, exactly once**,
   passing:

   - `body`: the **complete** markdown comment you drafted in step 6,
     exactly as it should appear on the issue.
   - `item_number`: the number of the issue you triaged. This safe output
     is configured with `target: "*"`, so you **must** name the target
     issue explicitly rather than relying on a default. For a frozen replay,
     use the issue number in the snapshot. Otherwise, use
     `${{ github.event.issue.number }}` for `issues.opened` runs and
     `${{ github.event.inputs.issue_number }}` for `workflow_dispatch`
     runs — whichever of the two is populated is the issue identified in
     "Issue to Triage" above.

   Call `add-comment` **at most once**, and never call both `add-comment`
   and `noop`.

### Dry Run Mode

If `${{ github.event.inputs.dry_run }}` is `true`, do **not** apply any
labels or issue type — skip `add-labels`, `set-issue-type`, and
`remove-labels` (steps 2–4 above). Still post the comment, but replace the
first heading `### Triage Summary` with `### [DRY RUN] Triage Summary` so it
is clear that nothing was applied. Every other rule applies unchanged — in
particular, the vulnerability gate still suppresses the comment entirely, so
a dry run on a vulnerability report results in a `noop` and no comment.

### No-op Fallback

Call the `noop` tool — and do **not** call `add-comment` — in either of
these two cases:

1. **The issue is a vulnerability report** (see the vulnerability gate
   above). Labels and issue type are still applied; only the comment is
   suppressed.

   ```json
   {"noop": {"message": "Triage comment suppressed: issue is a vulnerability report"}}
   ```

2. **There is nothing to say** — e.g. the issue already has an area label
   and an issue type, and there are no duplicates worth flagging.

   ```json
   {"noop": {"message": "No action needed: issue already has area and type labels"}}
   ```
