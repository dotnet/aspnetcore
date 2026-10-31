# Section guidelines for API review issues

## Background and Motivation

Explain the purpose and value of the new API so reviewers understand why the change is needed.

- Describe the problem clearly and concisely.
- Explain the current limitations or gaps.
- Link the originating issue and implementation pull request or commits.
- Focus on why the API is needed rather than how it is implemented.

Good:

> Previously, users were able to invoke JavaScript functions from .NET code using the `InvokeAsync` method from the `IJSRuntime` and `IJSObjectReference` interfaces. To perform any other JavaScript operation, they had to wrap it into a plain JavaScript function, deploy that function with their application, and invoke it via `InvokeAsync`. To reduce the need for such boilerplate code, we propose adding methods to the interop API to enable performing common operations directly.

Bad:

> Adding a string overload for Widget.ConfigureFactory.

## Usage Examples

Demonstrate how the proposed API is consumed so reviewers can evaluate its shape, functionality, performance, and usability.

- Use realistic, practical examples.
- Show simple and complex scenarios when both are relevant.
- Include synchronous and asynchronous variants when applicable.
- Use correctly labeled code fences.

Example:

```csharp
@inject IJSRuntime JSRuntime

string title = await JSRuntime.GetValueAsync<string>("document.title");
await JSRuntime.SetValueAsync("document.title", "Hello there");

IJSObjectReference chartRef = await JSRuntime.InvokeNewAsync("Chart", chartParameters);
var chartProperty = await chartRef.GetValueAsync<int>("somePropName");
```

## Proposed API

Provide the complete public API signature diff being proposed.

- Use ref-assembly format.
- Include complete namespace and type declarations.
- Prefix additions with `+` and removals with `-`.
- Include all overloads and extension methods.
- Show the complete type hierarchy when adding to existing interfaces.
- Use the `PublicAPI.Unshipped.txt` changes when available, but reconstruct them as complete ref-assembly declarations rather than pasting isolated baseline entries.
- For areas that do not produce ref assemblies, write the equivalent ref-assembly shape.

Simple example:

```diff
namespace Microsoft.AspNetCore.Http;

public static class HttpResponseWritingExtensions
{
+    public Task WriteAsync(this HttpResponse response, StringBuilder builder);
}
```

Complex example:

```diff
namespace Microsoft.JSInterop
{
    public interface IJSRuntime
    {
+        ValueTask<TValue> GetValueAsync<TValue>(string identifier);
+        ValueTask<TValue> GetValueAsync<TValue>(string identifier, CancellationToken cancellationToken);
+        ValueTask SetValueAsync<TValue>(string identifier, TValue value);
+        ValueTask SetValueAsync<TValue>(string identifier, TValue value, CancellationToken cancellationToken);
    }
}
```

## Alternative Designs (optional)

Show that other approaches were considered and explain why the proposal is preferred.

- Ask the user whether alternatives were considered when the available evidence does not say.
- Write `N/A` when the user confirms there are no alternative designs to document.
- Describe other API shapes considered.
- Compare analogous APIs where the evidence includes them.
- Explain the tradeoffs.
- State why the proposed approach was chosen.

Example:

> We considered supporting the additional operations with only the existing `InvokeAsync` method and selecting its behavior according to what JavaScript entity is found based on the `identifier`. However, this approach has clarity and predictability issues. There is also no general way to differentiate between normal and constructor functions in JavaScript.

## Risks (optional)

Identify concerns raised by the proposal or implementation.

- Ask the user whether they know of relevant risks when the available evidence does not identify any.
- Write `N/A` when the user confirms there are no risks to document.

Consider:

- Breaking changes
- Performance implications or regressions
- Security concerns
- Compatibility issues
- Potential misuse
- Impact on existing patterns or conventions

Example:

> The added interface methods have default implementations that throw `NotImplementedException` to avoid breaking builds of their implementors. Streamlining interop in this manner might also encourage inefficient interop calls.
