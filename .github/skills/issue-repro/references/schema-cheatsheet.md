# Repro Schema Quick Reference

Read this BEFORE generating JSON. Full schema: `references/repro-schema.json`.

## Required Top-Level Fields (always)

| Field | Type | Notes |
|-------|------|-------|
| `meta` | object | `schemaVersion` ("1.0"), `number` (int), `repo` ("dotnet/aspnetcore"), `analyzedAt` (ISO 8601) |
| `conclusion` | string | One of the `conclusionValue` enum (see below) |
| `notes` | string | Summary of what happened (min 10 chars) |
| `reproductionSteps` | array | At least 1 step |
| `environment` | object | `os`, `arch`, `dotnetVersion`, `aspnetcoreVersion`, `dockerUsed` (all required) |

## Conditionally Required by Conclusion

| Conclusion | Additional Required Fields |
|------------|---------------------------|
| `reproduced` | `output`, `versionResults`, `reproProject` |
| `not-reproduced` | `output`, `versionResults` |
| `needs-platform`, `needs-hardware`, `partial`, `inconclusive` | `blockers` (string array, min 1 item) |

## Step-Result Constraints by Conclusion

| Conclusion | Step Constraint |
|------------|----------------|
| `reproduced` | Must contain ≥1 step with `result: "failure"` or `"wrong-output"` |
| `not-reproduced` | Must contain ≥1 `"success"` step AND zero `"failure"`/`"wrong-output"` steps |

⚠️ For `not-reproduced`: if a setup step failed then succeeded on retry, record **only the final successful attempt**.

## Enum Values

| Enum | Values |
|------|--------|
| **conclusionValue** | `reproduced`, `not-reproduced`, `needs-platform`, `needs-hardware`, `partial`, `inconclusive` |
| **stepResult** | `success`, `failure`, `wrong-output`, `skip` |
| **stepLayer** | `setup`, `csharp`, `hosting`, `middleware`, `http`, `deployment` |
| **reproProject.type** | `webapi`, `mvc`, `razorpages`, `blazor-wasm`, `blazor-server`, `blazor-ssr`, `grpc`, `console`, `docker`, `test`, `existing`, `simulation` |
| **suggestedAction** | `needs-investigation`, `close-as-fixed`, `close-as-by-design`, `close-with-docs`, `close-as-duplicate`, `convert-to-discussion`, `request-info`, `keep-open` |

## Reproduction Step (each item in `reproductionSteps`)

Required: `stepNumber`, `description`, `layer`, `result`

```json
{
  "stepNumber": 1,
  "description": "Create minimal API project and make HTTP request to trigger bug",
  "command": "cd /tmp/aspnetcore/repro/12345 && curl -s http://localhost:5000/api",
  "exitCode": 0,
  "output": "HTTP/1.1 500 Internal Server Error\n...",
  "layer": "http",
  "result": "wrong-output"
}
```

### Layer Selection Guide

| Layer | Use when |
|-------|---------|
| `setup` | Project creation, `dotnet new`, package install, port setup |
| `csharp` | Writing/compiling C# code, `dotnet build` |
| `hosting` | `dotnet run`, app startup, DI configuration |
| `middleware` | Middleware pipeline execution (no HTTP request yet) |
| `http` | Making HTTP requests to the running app, checking responses |
| `deployment` | `dotnet publish`, Docker build/run, container deployment |

## Version Result (each item in `versionResults`)

Required: `version`, `source`, `result`

```json
{
  "version": "9.0.2",
  "source": "nuget",
  "result": "reproduced",
  "notes": "Reporter's version. Bug confirmed.",
  "platform": "host-macos-arm64"
}
```

`source`: `"nuget"` or `"source"`. `result`: `"reproduced"`, `"not-reproduced"`, `"error"`, `"not-tested"`.

## Output (required when reproduced or not-reproduced)

```json
"output": {
  "actionability": { "suggestedAction": "<enum>", "confidence": 0.0-1.0, "reason": "..." },
  "actions": [{ "type": "<actionType>", "description": "...", "risk": "low|medium|high" }],
  "proposedResponse": { "body": "GitHub comment markdown", "status": "ready|needs-human-edit|do-not-post" }
}
```

## Common Mistakes

1. **Missing `output` when `not-reproduced`** — `output` + `versionResults` required for BOTH conclusions.
2. **Missing `blockers` for blocked conclusions** — `needs-platform`, `needs-hardware`, `partial`, `inconclusive` all require `blockers[]`.
3. **`failure`/`wrong-output` steps with `not-reproduced`** — ALL steps must be `success` or `skip`.
4. **Missing `environment.dockerUsed`** — Always include, even if `false`. It's REQUIRED.
5. **Missing `proposedResponse.status`** — Must be `"ready"`, `"needs-human-edit"`, or `"do-not-post"`.
6. **Wrong `meta.repo`** — Must be `"dotnet/aspnetcore"` exactly.
7. **Missing `aspnetcoreVersion` in environment** — Required field, not optional.
8. **Extra fields** — `additionalProperties: false` everywhere. No extra keys.
9. **Null values** — Omit optional fields entirely. Never set to `null`.
10. **Step `result` = expectation** — `result` is TECHNICAL outcome. HTTP 500 with exitCode 0 = `"wrong-output"`.
