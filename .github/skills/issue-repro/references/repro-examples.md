# Bug Reproduction — Example Outputs

Reference examples showing valid `issue-repro` JSON output for different conclusion types.
Each example conforms to [`repro-schema.json`](repro-schema.json).

---

## Example 1: Middleware Bug — `reproduced`

**Scenario:** Reporter gets 401 on all endpoints because `UseAuthorization()` is called before `UseAuthentication()`.

```json
{
  "meta": {
    "schemaVersion": "1.0",
    "number": 12345,
    "repo": "dotnet/aspnetcore",
    "analyzedAt": "2026-01-15T10:30:00Z"
  },
  "inputs": {
    "triageFile": "artifacts/ai/triage/12345.json"
  },
  "conclusion": "reproduced",
  "notes": "Confirmed: calling UseAuthorization() before UseAuthentication() causes all [Authorize] endpoints to return 401, even with a valid JWT token. The fix is trivial — swap the middleware order. Reproduced on .NET 8.0.10 and .NET 9.0.2.",
  "assessment": "working-as-designed",
  "scope": "universal",
  "reproductionTime": "~8 minutes",
  "versionResults": [
    {
      "version": "8.0.10",
      "source": "nuget",
      "result": "reproduced",
      "platform": "host-macos-arm64",
      "notes": "Reporter's version. 401 returned with valid token."
    },
    {
      "version": "9.0.2",
      "source": "nuget",
      "result": "reproduced",
      "platform": "host-macos-arm64",
      "notes": "Latest. Still 401 — same middleware ordering issue."
    }
  ],
  "reproProject": {
    "type": "webapi",
    "tfm": "net8.0",
    "packages": [
      { "name": "Microsoft.AspNetCore.Authentication.JwtBearer", "version": "8.0.10" }
    ]
  },
  "reproductionSteps": [
    {
      "stepNumber": 1,
      "description": "Create minimal WebAPI with JWT auth using reporter's middleware order (UseAuthorization before UseAuthentication).",
      "layer": "setup",
      "command": "dotnet new webapi -n Repro12345 --no-openapi && cd Repro12345 && dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.10",
      "exitCode": 0,
      "output": "The template \"ASP.NET Core Web API\" was created successfully.\n  Restored Repro12345.csproj.",
      "result": "success"
    },
    {
      "stepNumber": 2,
      "description": "Write Program.cs with middleware in reporter's (incorrect) order: UseAuthorization before UseAuthentication.",
      "layer": "csharp",
      "filesCreated": [
        {
          "filename": "Program.cs",
          "description": "Minimal API with JWT auth — middleware in reporter's order to reproduce 401 issue.",
          "content": "var builder = WebApplication.CreateBuilder(args);\nbuilder.Services.AddAuthentication(\"Bearer\")\n    .AddJwtBearer(\"Bearer\", opts =>\n    {\n        opts.Authority = \"https://localhost\";\n        opts.TokenValidationParameters = new()\n        {\n            ValidateAudience = false,\n            ValidateIssuer = false,\n            ValidateLifetime = false,\n            ValidateIssuerSigningKey = false,\n            SignatureValidator = (token, _) => new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(token)\n        };\n    });\nbuilder.Services.AddAuthorization();\n\nvar app = builder.Build();\n\n// REPORTER'S ORDER: wrong — UseAuthorization before UseAuthentication\napp.UseAuthorization();\napp.UseAuthentication();\n\napp.MapGet(\"/public\", () => \"public\");\napp.MapGet(\"/secure\", () => \"secret\").RequireAuthorization();\n\napp.Run();"
        }
      ],
      "exitCode": 0,
      "result": "success"
    },
    {
      "stepNumber": 3,
      "description": "Build the project.",
      "layer": "csharp",
      "command": "dotnet build Repro12345",
      "exitCode": 0,
      "output": "Build succeeded.",
      "result": "success"
    },
    {
      "stepNumber": 4,
      "description": "Start app, request public endpoint (expect 200) then secure endpoint with token (expect 200 but gets 401).",
      "layer": "http",
      "command": "cd Repro12345 && dotnet run --no-build & sleep 3 && curl -s -o /dev/null -w '%{http_code}' http://localhost:5000/public && echo '' && curl -s -o /dev/null -w '%{http_code}' -H 'Authorization: Bearer eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0.eyJzdWIiOiJ0ZXN0In0.' http://localhost:5000/secure && kill %1 2>/dev/null",
      "exitCode": 0,
      "output": "200\n401",
      "result": "wrong-output"
    }
  ],
  "errorMessages": {
    "primaryError": "HTTP 401 Unauthorized returned on /secure endpoint with valid JWT token"
  },
  "environment": {
    "os": "macOS 15.3",
    "arch": "arm64",
    "dotnetVersion": "8.0.10",
    "dotnetSdkVersion": "8.0.404",
    "aspnetcoreVersion": "8.0.10",
    "dockerUsed": false
  },
  "output": {
    "actionability": {
      "suggestedAction": "close-as-by-design",
      "confidence": 0.85,
      "reason": "Reproduced as reported. Behavior is by-design — middleware ordering is documented. Workaround is trivial: swap UseAuthentication and UseAuthorization."
    },
    "workarounds": [
      "Call app.UseAuthentication() before app.UseAuthorization() in the middleware pipeline."
    ],
    "proposedResponse": {
      "status": "ready",
      "summary": "Confirmed 401 due to middleware order — workaround is to swap UseAuthentication/UseAuthorization.",
      "body": "Thanks for the detailed report. We've confirmed the issue.\n\nThe 401 occurs because `UseAuthorization()` is called before `UseAuthentication()`. Authentication establishes who the user is; authorization then checks what they're allowed to do. If auth runs first, the user is always anonymous.\n\n**Workaround** — use this middleware order:\n\n```csharp\napp.UseAuthentication();  // first: establish identity\napp.UseAuthorization();   // second: check permissions\n```\n\nWe're tracking a documentation improvement to make this ordering requirement more prominent in the getting-started guides."
    },
    "actions": [
      {
        "type": "update-labels",
        "description": "Add area-auth and Docs labels",
        "risk": "low",
        "confidence": 0.90,
        "labels": ["area-auth", "Docs"]
      }
    ]
  }
}
```

---

## Example 2: Bug — `not-reproduced` (request for info)

**Scenario:** Reporter says Blazor component doesn't update on parameter change, but cannot be reproduced with minimal project.

```json
{
  "meta": {
    "schemaVersion": "1.0",
    "number": 23456,
    "repo": "dotnet/aspnetcore",
    "analyzedAt": "2026-01-20T15:00:00Z"
  },
  "conclusion": "not-reproduced",
  "notes": "Created a minimal Blazor Server app with a component that takes a string parameter and displays it. Changing the parameter from the parent component correctly triggered OnParametersSet and re-rendered the UI. Cannot reproduce without more information about the reporter's specific component structure.",
  "scope": "unknown",
  "reproductionTime": "~12 minutes",
  "versionResults": [
    {
      "version": "8.0.10",
      "source": "nuget",
      "result": "not-reproduced",
      "platform": "host-macos-arm64",
      "notes": "Component re-renders correctly when parameters change."
    },
    {
      "version": "9.0.2",
      "source": "nuget",
      "result": "not-reproduced",
      "platform": "host-macos-arm64",
      "notes": "Same behavior — works correctly on latest."
    }
  ],
  "reproductionSteps": [
    {
      "stepNumber": 1,
      "description": "Create minimal Blazor Server app.",
      "layer": "setup",
      "command": "dotnet new blazorserver -n Repro23456 && cd Repro23456",
      "exitCode": 0,
      "output": "The template \"Blazor Server App\" was created successfully.",
      "result": "success"
    },
    {
      "stepNumber": 2,
      "description": "Create child component that displays a parameter value and logs parameter changes.",
      "layer": "csharp",
      "filesCreated": [
        {
          "filename": "Components/ChildComponent.razor",
          "description": "Child component with a string Parameter that logs changes.",
          "content": "<p>Value: @Value</p>\n\n@code {\n    [Parameter] public string Value { get; set; } = string.Empty;\n    \n    protected override void OnParametersSet()\n    {\n        Console.WriteLine($\"OnParametersSet called: Value={Value}\");\n    }\n}"
        }
      ],
      "exitCode": 0,
      "result": "success"
    },
    {
      "stepNumber": 3,
      "description": "Build and run, verify parameter changes trigger re-render.",
      "layer": "hosting",
      "command": "dotnet run --no-build",
      "exitCode": 0,
      "output": "info: Microsoft.Hosting.Lifetime[14]\n      Now listening on: https://localhost:7001\nOnParametersSet called: Value=initial\nOnParametersSet called: Value=updated",
      "result": "success"
    }
  ],
  "environment": {
    "os": "macOS 15.3",
    "arch": "arm64",
    "dotnetVersion": "8.0.10",
    "dotnetSdkVersion": "8.0.404",
    "aspnetcoreVersion": "8.0.10",
    "dockerUsed": false
  },
  "output": {
    "actionability": {
      "suggestedAction": "request-info",
      "confidence": 0.80,
      "reason": "Cannot reproduce with a minimal component. Likely requires specific component structure or parent interaction pattern not captured in the issue."
    },
    "proposedResponse": {
      "status": "ready",
      "summary": "Cannot reproduce — requesting minimal repro project.",
      "body": "Thanks for the report. We tried to reproduce this with a minimal Blazor Server component but were unable to observe the parameter-change issue — `OnParametersSet` fired correctly and the UI re-rendered as expected.\n\nCould you share:\n- A minimal reproduction project (see https://github.com/dotnet/aspnetcore/blob/main/docs/repro.md)\n- Whether this is Blazor Server or Blazor WebAssembly\n- Whether the parent component uses `@key` on the child\n- The output of `dotnet --info`\n\nThe component structure and how the parent triggers parameter changes may be key to reproducing this."
    },
    "actions": [
      {
        "type": "add-comment",
        "description": "Request minimal repro project and clarifying details",
        "risk": "low",
        "confidence": 0.90
      },
      {
        "type": "update-labels",
        "description": "Add Needs: Author Feedback label",
        "risk": "low",
        "confidence": 0.90,
        "labels": ["Needs: Author Feedback", "area-blazor"]
      }
    ],
    "missingInfo": [
      "Minimal reproduction project",
      "Blazor Server vs Blazor WebAssembly",
      "Whether @key is used on the child component",
      "Output of `dotnet --info`"
    ]
  }
}
```
