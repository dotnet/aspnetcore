# Route embedded language tooling

Route tooling is applied to strings used on APIs annotated with `[StringSyntax("Route")]`. The tooling consists of 5 components:

1. An analyzer that reports syntax problems in routes.
2. A classifier that colorizes route parts.
3. A brace matcher that highlights matching braces inside routes.
4. A highlighter that highlights route parameter names inside routes and matching arguments the route is used with.
5. A completion provider provides completion items for route constraints and parameter names.

## Roslyn integration

Route tooling uses public Roslyn APIs where possible and internal Roslyn APIs where necessary. Internal APIs are accessed via `Microsoft.CodeAnalysis.ExternalAccess.AspNetCore`, which follows Roslyn's standard external access pattern.

The classifier, brace matcher, and highlighter currently use internal APIs. The analyzer is a standard Roslyn analyzer, and the completion provider is a standard Roslyn completion provider.

The analyzer can be run from an editor or from the SDK command-line. Because of this, the analyzer avoids any dependency on `Microsoft.CodeAnalysis.ExternalAccess.AspNetCore`.

## Route pattern tree

A route pattern tree is shared between all components. It is parsed from the route string and has nodes and tokens for the various parts of a route. Additionally, the route pattern tree has a list containing any route syntax errors encountered while parsing the route.

Route parsing uses `IVirtualCharService`. This service provides a uniform view of a language's string token characters. It is easy to handle language string features, such as escaped chars.

## Future improvements

### Dependencies

Route tooling pushes Roslyn boundaries by being the first external project to use string syntax features. Ideally `Microsoft.CodeAnalysis.ExternalAccess.AspNetCore` is a temporary workaround. Making these features part of Roslyn's public API would allow ASP.NET Core to remove the external access dependency and source code copying.

- Reduce the amount of source copied from Roslyn.
  - String syntax detector
  - Virtual char service
  - Embedded syntax model
- Remove external access requirements by adding public features to Roslyn.
  - Classifier
  - Brace matcher
  - Highlighter

### Splitting projects

Splitting editor specific features out into a different assembly which references `Microsoft.CodeAnalysis.ExternalAccess.AspNetCore` would prevent analyzers accidentally using editor APIs.

### Completion provider

The completion provider doesn't support the user explicitly requesting completions. It isn't supported because CompletionProvider's loaded from projects don't support overriding description or customizing how text is added. This is needed to support completions when the user explicitly asks for completion. [It's fixed in VS 17.4](https://github.com/dotnet/roslyn/pull/61976).
