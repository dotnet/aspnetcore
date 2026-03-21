---
mode: edit
---

# Instructions for Filling in an API Review Issue

## Prerequisites

Before you begin filling out an API review issue, gather the following information:

* **Original issue** - The GitHub issue or feature request that prompted this API change
* **GitHub commit(s) containing the implementation** (if any) - Links to specific commits where the API was implemented
  * **Pay special attention to `PublicAPI.Unshipped.txt` files** - These files contain API changes and are crucial for the review

**ASK FOR CONFIRMATION**: Make a check-list of all the information (issue and commit). Mark the items that you have gathered. If any item is missing, ask the author to provide it before proceeding.

## Overview

The API review process ensures that new APIs follow common patterns and best practices while guiding engineers toward better API design decisions. The goal is to provide enough context for people working outside that feature area to understand what the change is about and give meaningful feedback.

## Sections

**DO NOT ADD ANY ADDITIONAL INFORMATION THAT IS NOT PART OF THE ORIGINAL ISSUE OR CODE CHANGES**
**AT THE END OF THE DOCUMENT, FOR ALL CONTENT ON EACH SECTION, YOU MUST JUSTIFY THE SOURCE FOR THAT CONTENT USING A QUOTE FRAGMENT FROM THE ORIGINAL ISSUE OR CODE CHANGES IN THIS FORMAT <<CONTENT>>: "<<QUOTE>>"**
**ASK FOR ADDITIONAL INFORMATION IF THERE IS NOT ENOUGH CONTEXT IN THE ORIGINAL ISSUE OR CODE CHANGES TO FILL IN A SECTION**
**YOU CAN FILL IN ANY SECTION WITH N/A IF THERE IS NO RELEVANT INFORMATION AVAILABLE**

Fill out each section following the format and guidelines below:

### 1. Background and Motivation

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

### 2. Proposed API

**Purpose**: Provide the specific public API signature diff that you are proposing.

**Requirements**:
- Use **ref-assembly format** (more readable and useful for API review discussions)
- Include the complete namespace and type declarations
- Show additions with `+` prefix in diff format
- If linking to generated ref-assembly code in a PR, that's acceptable
- For areas that don't produce ref-assemblies, write out what it would look like in ref-assembly format

**Format Example**:
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

**Key Points**:
- Include all overloads and extension methods
- Show the complete type hierarchy when adding to existing interfaces
- Reference the `PublicAPI.Unshipped.txt` files if available in your implementation commits

### 3. Usage Examples

**Purpose**: Demonstrate how the proposed API additions are meant to be consumed to validate that the API has the right shape to be functional, performant, and usable.

**Guidelines**:
- Provide realistic, practical code examples
- Show both simple and complex usage scenarios
- Include both synchronous and asynchronous variants (if applicable)
- Use proper C# code block formatting

**Example Format**:
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

### 4. Alternative Designs

**Purpose**: Show that you've considered other approaches and explain why the proposed design is the best option.

**Include**:
- Other API shapes you considered
- Comparison to analogous APIs in other ecosystems and libraries
- Trade-offs between different approaches
- Why the proposed approach was chosen over alternatives

**Example**:
> We considered supporting the additional operations with only the existing `InvokeAsync` method and selecting its behavior according to what JS entity is found based on the `identifier`. However, this approach has obvious UX issues (clarity, predictability). There is also no way to differentiate, in general, between "normal" and "constructor" functions in JavaScript.

### 5. Risks

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

## Quality Checklist

Before marking your issue as `api-ready-for-review`, ensure:

- [ ] **Clear description**: Includes a short description that will help reviewers not familiar with this area
- [ ] **Complete API specification**: All API changes are in ref-assembly format
- [ ] **Implementation reference**: Links to commits containing the implementation, especially `PublicAPI.Unshipped.txt` files
- [ ] **Adequate context**: Larger changes have more explanation and context
- [ ] **Usage examples**: Realistic code examples that demonstrate the API's intended use
- [ ] **Risk assessment**: Potential issues and breaking changes are identified
- [ ] **Champion identified**: Someone is assigned to champion this change in the API review meeting

## Process Notes

1. **Label progression**: Issues move from `api-suggestion` → `api-ready-for-review` → `api-approved`
2. **Team notification**: Notify @asp-net-api-reviews team when marking as `api-ready-for-review`
3. **Meeting attendance**: If your API is scheduled for review, you must have a representative in the meeting
4. **Implementation changes**: If changes to the original proposal are required during implementation, the review becomes obsolete and the process starts over

## Reference Resources

- [API Review Process Documentation](https://github.com/dotnet/aspnetcore/blob/main/docs/APIReviewProcess.md)
- [Framework Design Guidelines](https://github.com/dotnet/runtime/blob/master/docs/coding-guidelines/framework-design-guidelines-digest.md)
- [API Review Principles](https://github.com/dotnet/aspnetcore/blob/main/docs/APIReviewPrinciples.md)
- [Pending API Reviews](https://aka.ms/aspnet/apireviews)

## Template Structure

```markdown
## Background and Motivation

<!--
We welcome API proposals! We have a process to evaluate the value and shape of new API. There is an overview of our process [here](https://github.com/dotnet/aspnetcore/blob/main/docs/APIReviewProcess.md). This template will help us gather the information we need to start the review process.
First, please describe the purpose and value of the new API here.
-->

## Proposed API

<!--
Please provide the specific public API signature diff that you are proposing. For example:
```diff
namespace Microsoft.AspNetCore.Http;

public static class HttpResponseWritingExtensions
{
+    public Task WriteAsync(this HttpResponse response, StringBuilder builder);
}
```
You may find the [Framework Design Guidelines](https://github.com/dotnet/runtime/blob/master/docs/coding-guidelines/framework-design-guidelines-digest.md) helpful.
-->

## Usage Examples

<!--
Please provide code examples that highlight how the proposed API additions are meant to be consumed.
This will help suggest whether the API has the right shape to be functional, performant and useable.
You can use code blocks like this:
```csharp
// some lines of code here
```
-->

## Alternative Designs

<!--
Were there other options you considered, such as alternative API shapes?
How does this compare to analogous APIs in other ecosystems and libraries?
-->

## Risks

<!--
Please mention any risks that to your knowledge the API proposal might entail, such as breaking changes, performance regressions, etc.
-->
```