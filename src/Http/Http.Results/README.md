# ASP.NET Core Http.Results

Http.Results contains the in-framework implementations of the `IResult` interface returned from Minimal APIs route handler delegates, e.g. `OkHttpResult`, `NoContentHttpResult`, etc.

## Development Setup

The `Results<TResult1, TResult2, TResultN>` union types are generated. Modify and run the [ResultsOfTGenerator](tools/ResultsOfTGenerator/) tool to generate an updated `ResultsOfT.cs` class file.

Run the following command in `src\Http\Http.Results\tools\ResultsOfTGenerator`:

```
dotnet run
```