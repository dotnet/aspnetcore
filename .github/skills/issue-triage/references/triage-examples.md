# Triage Examples

Reference examples showing valid `issue-triage` JSON output for different issue types.
Each example conforms to [`triage-schema.json`](triage-schema.json).

---

## Example 1: Bug — Middleware Order Issue

**Scenario:** Reporter gets 401 on every request even after adding authentication.

```json
{
  "meta": {
    "schemaVersion": "1.0",
    "number": 12345,
    "repo": "dotnet/aspnetcore",
    "analyzedAt": "2026-01-15T10:00:00Z",
    "currentLabels": ["bug"]
  },
  "summary": "Reporter gets 401 Unauthorized on all endpoints after adding JWT authentication to a .NET 9 minimal API app. UseAuthorization is configured but requests with valid tokens are rejected.",
  "classification": {
    "type": { "value": "bug", "confidence": 0.75 },
    "area": { "value": "area-auth", "confidence": 0.90 },
    "feature": { "value": "feature-minimal-actions", "confidence": 0.80 }
  },
  "evidence": {
    "reproEvidence": {
      "stepsToReproduce": [
        "Create a new .NET 9 minimal API project",
        "Add AddAuthentication().AddJwtBearer() to services",
        "Add UseAuthorization() to the pipeline",
        "Add [Authorize] to an endpoint",
        "Make a request with a valid JWT token"
      ],
      "codeSnippets": [
        "app.UseAuthorization();\napp.UseAuthentication();",
        "app.MapGet(\"/secret\", [Authorize] () => \"secret\");"
      ],
      "environmentDetails": ".NET 9.0.2, Windows 11 x64"
    },
    "bugSignals": {
      "severity": "high",
      "errorType": "wrong-output",
      "errorMessage": "401 Unauthorized on all requests",
      "reproQuality": "partial",
      "dotnetVersion": "9.0.2",
      "aspnetcoreVersion": "9.0.2"
    }
  },
  "analysis": {
    "summary": "The issue is a middleware ordering mistake: UseAuthorization() is called before UseAuthentication() in the code snippet. Auth middleware processes in pipeline order — authorization runs before authentication can establish the user identity, so the user is never authenticated and all [Authorize] endpoints return 401.",
    "rationale": "Classified as bug (medium confidence) because the documentation does not clearly warn about this ordering requirement in the getting started guides. The behavior itself is by-design but the trap is common enough that it represents a documentation gap. Classified as area-auth with feature-minimal-actions since the reporter is using minimal APIs with JWT auth.",
    "keySignals": [
      {
        "text": "app.UseAuthorization();\napp.UseAuthentication();",
        "source": "issue body code snippet",
        "interpretation": "UseAuthorization comes before UseAuthentication — this is the root cause."
      }
    ],
    "codeInvestigation": [
      {
        "file": "src/Security/Authentication/Core/src/AuthenticationMiddleware.cs",
        "finding": "AuthenticationMiddleware.Invoke sets HttpContext.User. Must run before authorization middleware checks the user identity.",
        "relevance": "direct"
      },
      {
        "file": "src/Security/Authorization/Policy/src/AuthorizationMiddleware.cs",
        "finding": "AuthorizationMiddleware.Invoke checks HttpContext.User for authorization. If called before AuthenticationMiddleware, User is unauthenticated.",
        "relevance": "direct"
      }
    ],
    "resolution": {
      "hypothesis": "Middleware order issue: UseAuthentication must be called before UseAuthorization.",
      "proposals": [
        {
          "title": "Reorder middleware",
          "description": "Call UseAuthentication() before UseAuthorization() in the pipeline.",
          "category": "workaround",
          "codeSnippet": "app.UseAuthentication();\napp.UseAuthorization();",
          "validated": "yes",
          "confidence": 0.99,
          "effort": "trivial"
        }
      ],
      "recommendedProposal": "Reorder middleware",
      "recommendedReason": "Trivial one-line fix, fully resolves the issue."
    }
  },
  "output": {
    "actionability": {
      "suggestedAction": "close-with-docs",
      "confidence": 0.80,
      "reason": "The behavior is by-design but the ordering requirement is not clearly documented in the getting-started guides. Workaround is trivial."
    },
    "actions": [
      {
        "type": "add-comment",
        "description": "Explain middleware ordering and provide working example",
        "risk": "low",
        "confidence": 0.95,
        "comment": "Thanks for the report!\n\nThe issue is the middleware ordering — `UseAuthentication()` must be called **before** `UseAuthorization()`. Authentication establishes who the user is; authorization then checks what they're allowed to do.\n\nHere's the correct order:\n\n```csharp\napp.UseRouting();\napp.UseAuthentication();  // first\napp.UseAuthorization();   // second\napp.MapControllers();\n```\n\nThis should resolve the 401 responses. See the [security docs](https://learn.microsoft.com/aspnet/core/security/) for more details."
      },
      {
        "type": "update-labels",
        "description": "Add area-auth and feature-minimal-actions labels",
        "risk": "low",
        "confidence": 0.90,
        "labels": ["area-auth", "feature-minimal-actions"]
      }
    ]
  }
}
```

---

## Example 2: Feature Request — Blazor Component API

**Scenario:** Reporter requests a built-in way to debounce input events in Blazor components.

```json
{
  "meta": {
    "schemaVersion": "1.0",
    "number": 23456,
    "repo": "dotnet/aspnetcore",
    "analyzedAt": "2026-01-20T14:30:00Z"
  },
  "summary": "Reporter requests a first-class debounce utility for Blazor input event handlers, currently requiring manual Timer or CancellationToken management for every component that needs debounced input.",
  "classification": {
    "type": { "value": "feature-request", "confidence": 0.95 },
    "area": { "value": "area-blazor", "confidence": 0.95 },
    "feature": { "value": "feature-blazor-component-model", "confidence": 0.85 }
  },
  "evidence": {
    "reproEvidence": {
      "codeSnippets": [
        "@code {\n  private CancellationTokenSource? _cts;\n  private async Task OnInput(ChangeEventArgs e)\n  {\n    _cts?.Cancel();\n    _cts = new CancellationTokenSource();\n    try\n    {\n      await Task.Delay(300, _cts.Token);\n      // do search\n    }\n    catch (OperationCanceledException) { }\n  }\n}"
      ]
    }
  },
  "analysis": {
    "summary": "This is a genuine feature gap. The manual CancellationToken pattern is verbose and error-prone. No built-in debounce utility exists in Blazor's component model. The feature would fit well as an extension method or a helper class.",
    "rationale": "Clearly a feature request — the reporter isn't reporting broken behavior, they're requesting new functionality. High confidence in area-blazor/feature-blazor-component-model since it's about the component event model.",
    "codeInvestigation": [
      {
        "file": "src/Components/Components/src/EventCallbackFactory.cs",
        "finding": "EventCallbackFactory creates EventCallback wrappers but has no debounce/throttle support.",
        "relevance": "related"
      }
    ],
    "resolution": {
      "proposals": [
        {
          "title": "Manual CancellationToken workaround",
          "description": "Use a CancellationTokenSource per component to cancel pending debounced work.",
          "category": "workaround",
          "codeSnippet": "@implements IDisposable\n@code {\n  private CancellationTokenSource _cts = new();\n  \n  private async Task OnInput(ChangeEventArgs e)\n  {\n    _cts.Cancel();\n    _cts = new CancellationTokenSource();\n    try\n    {\n      await Task.Delay(300, _cts.Token);\n      await PerformSearch(e.Value?.ToString());\n    }\n    catch (OperationCanceledException) { }\n  }\n  \n  public void Dispose() => _cts.Dispose();\n}",
          "validated": "yes",
          "confidence": 0.90,
          "effort": "small"
        }
      ]
    }
  },
  "output": {
    "actionability": {
      "suggestedAction": "keep-open",
      "confidence": 0.80,
      "reason": "Valid feature request with no existing built-in alternative. Workaround exists but is verbose."
    },
    "actions": [
      {
        "type": "update-labels",
        "description": "Label as enhancement for the Blazor component model",
        "risk": "low",
        "confidence": 0.90,
        "labels": ["enhancement", "area-blazor", "feature-blazor-component-model"]
      }
    ]
  }
}
```
