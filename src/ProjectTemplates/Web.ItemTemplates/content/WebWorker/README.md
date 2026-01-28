# WebWorker Template

This template adds WebWorker support to an existing Blazor WebAssembly project, enabling you to run .NET code in a separate dotnet runtime without blocking the UI.

## Usage

```bash
dotnet new webworker
```

This adds the **WorkerClient** project—a reusable library that handles all WebWorker communication.
