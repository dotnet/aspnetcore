# Section Guidelines for API Review Issues

Detailed guidance for each section of the API review issue. Each section serves a specific purpose in the review process.

## 1. Background and Motivation

**Purpose**: Explain the purpose and value of the new API to help reviewers understand why this change is needed.

**Guidelines**:
- Provide a clear, concise description of the problem being solved
- Explain the current limitations or gaps that this API addresses
- Reference the original issue that prompted this change
- Focus on the "why" rather than the "how"

**Good Example**:
> Previously, users were able to invoke JavaScript functions from .NET code using the `InvokeAsync` method from the `IJSRuntime` and `IJSObjectReference` interfaces. To perform any other JavaScript operation, they had to wrap it into a plain JavaScript function, deploy that function with their application, and invoke it via `InvokeAsync`. To reduce the need for such boilerplate code, we propose adding methods to the interop API to enable performing common operations directly.

**Bad Example**:
> Adding a string overload for Widget.ConfigureFactory.

## 2. Proposed API

**Purpose**: Provide the specific public API signature diff being proposed.

**Requirements**:
- Use **ref-assembly format** (more readable for API review discussions)
- Include the complete namespace and type declarations
- Show additions with `+` prefix in diff format
- Linking to generated ref-assembly code in a PR is acceptable
- For areas that don't produce ref-assemblies, write out what it would look like in ref-assembly format
- Include all overloads and extension methods
- Show the complete type hierarchy when adding to existing interfaces
- Reference `PublicAPI.Unshipped.txt` files if available

**Simple Format Example**:
```diff
namespace Microsoft.AspNetCore.Http;

public static class HttpResponseWritingExtensions
{
+    public Task WriteAsync(this HttpResponse response, StringBuilder builder);
}
```

**Complex API Example**:
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

## 3. Usage Examples

**Purpose**: Demonstrate how the proposed API is meant to be consumed to validate shape, functionality, performance, and usability.

**Guidelines**:
- Provide realistic, practical code examples
- Show both simple and complex usage scenarios
- Include both synchronous and asynchronous variants (if applicable)
- Use proper C# code block formatting

**Example**:
```csharp
@inject IJSRuntime JSRuntime

// Simple property access
string title = await JSRuntime.GetValueAsync<string>("document.title");

// Setting values
await JSRuntime.SetValueAsync("document.title", "Hello there");

// Constructor invocation
IJSObjectReference chartRef = await JSRuntime.InvokeNewAsync("Chart", chartParams);

// Working with object references
var someChartProperty = await chartRef.GetValueAsync<int>("somePropName");
```

## 4. Alternative Designs

**Purpose**: Show that other approaches were considered and explain why the proposed design is the best option.

**Include**:
- Other API shapes considered
- Comparison to analogous APIs in other ecosystems and libraries
- Trade-offs between different approaches
- Why the proposed approach was chosen over alternatives

**Example**:
> We considered supporting the additional operations with only the existing `InvokeAsync` method and selecting its behavior according to what JS entity is found based on the `identifier`. However, this approach has obvious UX issues (clarity, predictability). There is also no way to differentiate, in general, between "normal" and "constructor" functions in JavaScript.

## 5. Risks

**Purpose**: Identify potential issues or concerns with the proposed API.

**Consider**:
- Breaking changes to existing code
- Performance implications or regressions
- Security concerns
- Compatibility issues
- Potential for misuse
- Impact on existing patterns or conventions

**Example**:
> The added interface methods have default implementations (which throw `NotImplementedException`) to avoid breaking builds of their implementors. A possible criticism of the feature is that streamlining interop in this manner might motivate misuse of inefficient interop calls.
