# WebWorker Template

This template adds WebWorker support to an existing Blazor WebAssembly project, enabling you to run .NET code in a background thread without blocking the UI.

## Usage

### Full Template (with examples)

```bash
dotnet new webworker
```

This adds:
- **WebWorkerTemplate.WorkerClient/** - The infrastructure project containing the WebWorker client
- **Worker/** - Example [JSExport] worker class (GitHubWorker)  
- **Models/** - Example model classes for the demo
- **Pages/** - Example Razor page demonstrating WebWorker usage

### Infrastructure Only

```bash
dotnet new webworker --empty
```

This adds only the WorkerClient infrastructure project without any example files.
