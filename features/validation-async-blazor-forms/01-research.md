# Async Validation for Blazor Forms — Research Document

## Table of Contents

1. [Problem Statement](#1-problem-statement)
2. [Current Blazor Forms Architecture](#2-current-blazor-forms-architecture)
3. [New Validation Package (Microsoft.Extensions.Validation)](#3-new-validation-package-microsoftextensionsvalidation)
4. [BCL Async Validation Design Proposal](#4-bcl-async-validation-design-proposal)
5. [Community Demand & Discussion History](#5-community-demand--discussion-history)
6. [Async Validation in Other Frameworks](#6-async-validation-in-other-frameworks)
7. [Existing Workarounds & Third-Party Libraries](#7-existing-workarounds--third-party-libraries)
8. [UI Patterns for Pending Validation](#8-ui-patterns-for-pending-validation)
9. [Key Technical Challenges](#9-key-technical-challenges)
10. [Summary of Integration Points](#10-summary-of-integration-points)

---

## 1. Problem Statement

Blazor form validation is entirely synchronous today. The `EditContext.Validate()` method fires a synchronous `EventHandler<ValidationRequestedEventArgs>` event and returns `bool`. There is no way to:

- Perform async validation (e.g., check username uniqueness via HTTP call) as part of the standard validation pipeline
- Report "pending" validation state to the UI (spinners, disabled buttons, "Checking…" messages)
- Await completion of validation tasks before determining form validity on submit

This has been a top community request since 2019 (see [issue #7680](https://github.com/dotnet/aspnetcore/issues/7680), opened shortly after forms were introduced in [PR #7614](https://github.com/dotnet/aspnetcore/pull/7614)). The original PR explicitly noted "async validation" as a planned enhancement.

**Use cases:**
- Username/email uniqueness checks
- Server-side business rule validation
- FluentValidation async validators
- Any validation requiring I/O (database, HTTP, file system)

---

## 2. Current Blazor Forms Architecture

### 2.1 Directory Layout

```
src/Components/
├── Forms/src/                           # Core validation & EditContext logic
│   ├── EditContext.cs                   # Central validation orchestrator (sealed)
│   ├── ValidationMessageStore.cs        # Stores messages per field per validator
│   ├── DataAnnotationsValidator.cs      # Component enabling DataAnnotations
│   ├── FieldIdentifier.cs              # Readonly struct: (Model, FieldName)
│   ├── FieldState.cs                    # Internal: IsModified + message stores
│   ├── EditContextProperties.cs         # Extensibility bag
│   ├── FieldChangedEventArgs.cs
│   ├── ValidationRequestedEventArgs.cs
│   ├── ValidationStateChangedEventArgs.cs
│   └── EditContextDataAnnotationsExtensions.cs  # Wires up DataAnnotations
│
└── Web/src/Forms/                       # UI components & binding
    ├── EditForm.cs                      # <form> wrapper, cascades EditContext
    ├── InputBase.cs                     # Generic base for all inputs
    ├── InputText.cs, InputNumber.cs, etc.
    ├── ValidationMessage.cs             # Per-field error display
    ├── ValidationSummary.cs             # All-errors summary
    └── FieldCssClassProvider.cs         # CSS class logic (valid/invalid/modified)
```

### 2.2 Core Classes

#### EditContext (sealed)
- **Constructor**: `EditContext(object model)` — creates context for model object
- **`Validate()`**: `bool Validate()` — fires sync `OnValidationRequested`, returns `!GetValidationMessages().Any()`
- **Events** (all synchronous `EventHandler<T>`):
  - `OnFieldChanged` — fired when `NotifyFieldChanged()` called
  - `OnValidationRequested` — fired by `Validate()`
  - `OnValidationStateChanged` — fired by `NotifyValidationStateChanged()`
- **State**: `Dictionary<FieldIdentifier, FieldState>` — sparse field tracking
- **Key methods**: `Field()`, `NotifyFieldChanged()`, `IsModified()`, `IsValid()`, `GetValidationMessages()`, `MarkAsUnmodified()`

#### ValidationMessageStore
- Holds messages for an EditContext, keyed by `FieldIdentifier`
- Multiple stores can attach to one EditContext (one per validator)
- Methods: `Add()`, `Clear()`, indexer `this[FieldIdentifier]`

#### EditForm
- Renders `<form>`, cascades `EditContext` to children
- Parameters: `Model`, `EditContext`, `OnSubmit`, `OnValidSubmit`, `OnInvalidSubmit`
- **`HandleSubmitAsync()`** (line ~204):
  ```csharp
  var isValid = _editContext.Validate(); // This will likely become ValidateAsync later
  ```
  The comment explicitly marks this as a future async integration point.

#### InputBase<TValue>
- Abstract generic base for all input components
- Handles value binding, parsing, and validation integration
- `TryParseValueFromString()` — abstract, synchronous
- Subscribes to `EditContext.OnValidationStateChanged` for re-renders
- Manages CSS classes via `FieldCssClassProvider`

#### DataAnnotationsValidator
- Component that calls `EditContext.EnableDataAnnotationsValidation(IServiceProvider)`
- Subscribes to `OnFieldChanged` → `Validator.TryValidateProperty()`
- Subscribes to `OnValidationRequested` → `Validator.TryValidateObject()` + `IValidatableInfo.ValidateAsync()`

#### EditContextDataAnnotationsExtensions
- **Critical code** — already calls `_validatorTypeInfo.ValidateAsync()` but **throws if it doesn't complete synchronously**:
  ```csharp
  var validationTask = _validatorTypeInfo.ValidateAsync(_editContext.Model, validateContext, CancellationToken.None);
  if (!validationTask.IsCompleted)
      throw new InvalidOperationException("Async validation is not supported");
  ```
- This shows the architecture is *partially prepared* for async but blocked by the sync event model.

### 2.3 Validation Pipeline Flow

```
User types → InputBase.CurrentValueAsString setter
  → TryParseValueFromString() [sync]
  → EditContext.NotifyFieldChanged()
  → OnFieldChanged event → DataAnnotationsValidator
    → Validator.TryValidateProperty() [sync]
    → ValidationMessageStore.Add/Clear
  → EditContext.NotifyValidationStateChanged()
  → InputBase/ValidationMessage/ValidationSummary → StateHasChanged()

Form submit → EditForm.HandleSubmitAsync()
  → EditContext.Validate() [sync]
  → OnValidationRequested event → DataAnnotationsValidator
    → Validator.TryValidateObject() [sync]
    → IValidatableInfo.ValidateAsync() [sync-over-async, throws if truly async]
  → OnValidSubmit / OnInvalidSubmit
```

### 2.4 CSS Class System

`FieldCssClassProvider.GetFieldCssClass()` returns:
- `"modified valid"` / `"modified invalid"` — for touched fields
- `"valid"` / `"invalid"` — for untouched fields

No concept of "pending" state exists today.

---

## 3. New Validation Package (Microsoft.Extensions.Validation)

### 3.1 Overview

A new async-first validation infrastructure in `src/Validation/` designed for Minimal APIs and Blazor. Currently marked `[Experimental("ASP0029")]`.

### 3.2 Core Interfaces & Classes

#### IValidatableInfo
```csharp
public interface IValidatableInfo
{
    Task ValidateAsync(object? value, ValidateContext context, CancellationToken cancellationToken);
}
```
Fully async from the ground up. All validation is `Task`-based with `CancellationToken` support.

#### ValidateContext
```csharp
public class ValidateContext
{
    public required ValidationContext ValidationContext { get; set; }
    public string CurrentValidationPath { get; set; } = string.Empty;
    public required ValidationOptions ValidationOptions { get; set; }
    public Dictionary<string, string[]>? ValidationErrors { get; set; }
    public int CurrentDepth { get; set; }
    public event Action<ValidationErrorContext>? OnValidationError;  // Event-based error streaming
}
```

#### ValidationErrorContext
```csharp
public readonly struct ValidationErrorContext
{
    public required string Name { get; init; }
    public required string Path { get; init; }          // e.g., "Customer.Address.Street"
    public required IReadOnlyList<string> Errors { get; init; }
    public required object? Container { get; init; }
}
```

#### ValidatableTypeInfo / ValidatablePropertyInfo / ValidatableParameterInfo
Abstract base classes implementing `IValidatableInfo`. The validation algorithm:
1. Validate all member properties (async recursion)
2. Validate inherited members
3. Validate type-level attributes (**currently sync** — calls `attribute.GetValidationResult()`)
4. Validate `IValidatableObject` interface (**currently sync**)

**Key limitation**: While the *orchestration* is async, the actual attribute validation calls `GetValidationResult()` synchronously. True async attribute validation requires BCL changes (see §4).

### 3.3 Source Generator

The source generator (`src/Validation/gen/`):
- Finds `AddValidation()` calls via method interception
- Discovers validatable types in endpoint parameters
- Generates `GeneratedValidatablePropertyInfo` / `GeneratedValidatableTypeInfo` subclasses
- Generates resolver inserted at position 0 in the resolver chain
- Uses `ValidationAttributeCache` with `ConcurrentDictionary` for reflection caching

### 3.4 Blazor Integration Points

The Validation package is already used by Blazor forms via `EditContextDataAnnotationsExtensions`:
- It resolves `IValidatableInfo` for the model type
- Calls `ValidateAsync()` but throws if truly async
- The `OnValidationError` event is designed for real-time error streaming to Blazor UI

### 3.5 Key Design Features for Async

- **Event-based error streaming**: `ValidateContext.OnValidationError` fires as each error is found
- **Cancellation**: All methods accept `CancellationToken`
- **Depth tracking**: Prevents stack overflow from circular references (max depth: 32)
- **Path tracking**: Full dotted paths for nested errors (`"Order.Items[0].Price"`)

---

## 4. BCL Async Validation Design Proposal

Source: [dotnet/designs PR #363](https://github.com/dotnet/designs/pull/363) by @halter73

### 4.1 Problem

The entire `ValidationAttribute` → `GetValidationResult` → `IsValid` chain in `System.ComponentModel.DataAnnotations` is synchronous. There's no way to write a `ValidationAttribute` that performs async work.

### 4.2 Chosen Approach: AsyncValidationAttribute

**Option C** was chosen — a new `AsyncValidationAttribute` class deriving from `ValidationAttribute`:

```csharp
namespace System.ComponentModel.DataAnnotations;

public abstract class AsyncValidationAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        throw new NotSupportedException(
            $"The validation attribute '{GetType().Name}' supports only asynchronous validation. " +
            "Use the async validation APIs (e.g., Validator.TryValidateObjectAsync).");
    }

    protected abstract ValueTask<ValidationResult?> IsValidAsync(
        object? value,
        ValidationContext validationContext,
        CancellationToken cancellationToken);
}
```

**Behavior matrix:**

| Attribute Type | Sync Path (`GetValidationResult`) | Async Path (`GetValidationResultAsync`) |
|---|---|---|
| Traditional `ValidationAttribute` (overrides `IsValid`) | ✅ Works | ✅ Default delegates to sync |
| `AsyncValidationAttribute` (overrides `IsValidAsync`) | ❌ Throws `NotSupportedException` | ✅ Calls `IsValidAsync` |
| `ValidationAttribute` overriding both | ✅ Uses `IsValid` | ✅ Uses `IsValidAsync` |

### 4.3 New Base Class Methods

On `ValidationAttribute`:
- `protected virtual ValueTask<ValidationResult?> IsValidAsync(object?, ValidationContext, CancellationToken)` — default wraps sync `IsValid`
- `public ValueTask<ValidationResult?> GetValidationResultAsync(object?, ValidationContext, CancellationToken)`

Uses `ValueTask<>` to avoid allocations when the path is synchronous.

### 4.4 New Async Validator API

Async counterparts for all `Validator` static methods:
- `TryValidateObjectAsync`, `TryValidatePropertyAsync`, `TryValidateValueAsync`
- `ValidateObjectAsync`, `ValidatePropertyAsync`, `ValidateValueAsync`

### 4.5 IAsyncValidatableObject

```csharp
public interface IAsyncValidatableObject : IValidatableObject
{
    IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext ctx)
        => throw new NotSupportedException("Use async validation APIs.");

    IAsyncEnumerable<ValidationResult> ValidateAsync(
        ValidationContext validationContext,
        CancellationToken cancellationToken = default);
}
```

### 4.6 Implementation Phases (from proposal)

1. **Phase 1**: Core `ValidationAttribute` async methods + `AsyncValidationAttribute` + async `Validator` methods (BCL/runtime)
2. **Phase 2**: ASP.NET Core Minimal API Validation — call `GetValidationResultAsync` instead of `GetValidationResult` in `ValidatablePropertyInfo`, `ValidatableTypeInfo`, `ValidatableParameterInfo`
3. **Phase 3**: Blazor async support — `EditContext.ValidateAsync()`, async event handlers, `EditForm` update
4. **Phase 4**: MVC async (future/deferred — large scope)
5. **Phase 5**: Options validation async (future/deferred — major architectural shift)

### 4.7 Related Issues

- [dotnet/runtime#121536](https://github.com/dotnet/runtime/issues/121536) — Runtime-side tracking
- [dotnet/aspnetcore#64609](https://github.com/dotnet/aspnetcore/issues/64609) — "Support async validation in minimal APIs" (assigned to @halter73, milestone `.NET 11 Planning`)

---

## 5. Community Demand & Discussion History

### 5.0 Tracking Epic — Issue #64892 (Dec 2025)

[dotnet/aspnetcore#64892](https://github.com/dotnet/aspnetcore/issues/64892) — **"Blazor Form Validation Enhancements"** epic by @javiercn. Milestone: **11.0-preview4**, assigned to @oroztocil. 7👍.

**Three key areas:**
1. **Client-Side Validation without Interactivity** — enable DataAnnotation-based client validation for statically rendered forms (no circuit/WASM needed)
2. **Async Validation Support** — extend EditContext API for async validation, pending task tracking, and UI feedback states
3. **Localization for Validation Messages** — improve localization support for data annotation attributes

**Sub-issues:**
- [#51040](https://github.com/dotnet/aspnetcore/issues/51040) — Client validation without circuit (11.0-preview4, @oroztocil)
- [#7680](https://github.com/dotnet/aspnetcore/issues/7680) — Async validation enhancements (11.0-preview4, @oroztocil)
- [#12158](https://github.com/dotnet/aspnetcore/issues/12158) — Data annotations localization (11.0-preview3, @oroztocil)

### 5.1 Issue #7680 — Original Async Validation Issue (2019)

[dotnet/aspnetcore#7680](https://github.com/dotnet/aspnetcore/issues/7680) — opened by @SteveSandersonMS right after forms were introduced.

**Original design sketch:**
- `editContext.AddValidationTask(FieldIdentifier f, Task t)` — registers async validation tasks
- Tasks stored in `HashSet<Task>` on both `FieldState` and `EditContext`
- `editContext.HasPendingValidationTasks()` — per-field and per-context
- `editContext.ValidateAsync()` — awaits all tasks, returns validity
- Validation libraries can register/cancel tasks and issue `ValidationResultsChanged` notifications
- UI can determine "pending" state per-field and per-form

**Community feedback highlights:**
- Blocking FluentValidation async validators ([Blazored/FluentValidation#38](https://github.com/Blazored/FluentValidation/issues/38))
- Cannot use `OnFieldChanged` for async work without blocking the app
- Request for `OnValidationRequestedAsync` event (async handler for form submission)
- Request for `IsValidatingChanged` callback on `EditForm` for UI feedback
- Static SSR forms would benefit from async validation too
- Dan Roth confirmed it should work "out of the box with EditForm and DataAnnotationsValidator" (Feb 2026)

### 5.2 Issue #40244 — Community Request (2022)

[dotnet/aspnetcore#40244](https://github.com/dotnet/aspnetcore/issues/40244)

- @mrpmorris requested async form validation support
- @SteveSandersonMS provided a manual workaround example using `ValidationMessageStore` + `Task.Delay`
- @mrpmorris later reconsidered: "I don't think Blazor should support async validation at all... it should be a UI concern" — but the workaround is complex
- Closed as resolved, duped to #7680

### 5.3 Issue #51501 — ValidateAsync Proposal (2023)

[dotnet/aspnetcore#51501](https://github.com/dotnet/aspnetcore/issues/51501)

Proposed approach:
```csharp
public event Func<object?, EventArgs, Task> OnValidationRequestedAsync;

public async Task<bool> ValidateAsync()
{
    OnValidationRequested?.Invoke(this, ValidationRequestedEventArgs.Empty);
    await Task.WhenAll(OnValidationRequestedAsync.GetInvocationList()
        .Cast<Func<object?, EventArgs, Task>>()
        .Select(invocation => invocation(this, EventArgs.Empty)));
    return !GetValidationMessages().Any();
}
```

Closed as duplicate of #7680.

### 5.4 Issue #31905 — FluentValidation Author's Request (2021)

[dotnet/aspnetcore#31905](https://github.com/dotnet/aspnetcore/issues/31905) — **"Please reconsider allowing async model validation"** by @JeremySkinner (FluentValidation author). **227👍, 27❤️** — the most upvoted async validation issue.

**Key points from the FluentValidation author:**
- FluentValidation supports sync and async execution, but ASP.NET's validation pipeline is sync-only
- Async rules are forced to run synchronously when integrated with ASP.NET, losing the library's full feature set
- FluentValidation must maintain two code paths (sync + async) largely because of ASP.NET's sync constraint
- Needed changes: `ObjectModelValidator.Validate` → async, `ValidationVisitor.Validate` → async, `IModelValidator.Validate` → async
- Offered to help implement the changes
- Milestone: **.NET 11 Planning**

### 5.5 Issue #64609 — Minimal API Async Validation (2025)

[dotnet/aspnetcore#64609](https://github.com/dotnet/aspnetcore/issues/64609) — **"Support async validation in minimal APIs"**, tracking the Minimal API side. Assigned to @halter73, milestone **.NET 11 Planning**. Depends on [dotnet/runtime#121536](https://github.com/dotnet/runtime/issues/121536).

### 5.6 Blazored/FluentValidation #38 — The Async Deadlock Bug (2020–ongoing)

[Blazored/FluentValidation#38](https://github.com/Blazored/FluentValidation/issues/38) — **"Async Validator not working correctly"**

This issue is the canonical example of the async validation pain point in the Blazor ecosystem:

**Root cause:** `EditForm` calls `EditContext.Validate()` synchronously → fires `OnValidationRequested` → Blazored's handler calls `validator.ValidateAsync()` but the event handler is `async void` → the form submits *before* async validation completes.

**User-reported impacts:**
- Forms submit with invalid data when async validators are used
- FluentValidation's `MustAsync()` rules run *after* the form has already been submitted
- On Blazor WASM, `HttpClient` is async-only — cannot do sync-over-async for server calls at all
- Thread blocking workarounds (`Task.Run().GetAwaiter().GetResult()`) cause UI freezes

**Workarounds attempted by the community:**
1. **Sync fallback**: Change `ValidateAsync` to `Validate` — loses async capability entirely
2. **Sync-over-async**: `Task.Run(() => validator.ValidateAsync(context)).GetAwaiter().GetResult()` — blocks UI thread
3. **Fake message hack**: Add a placeholder validation message *before* starting async work (forces form to appear invalid), then replace with real results — fragile
4. **Server-side validation**: Move async validation to the API layer, return `ValidationProblemDetails`, display errors via a separate component — works but splits validation logic
5. **Custom `EditForm`**: Fork `EditForm` to call `ValidateAsync` in `HandleSubmitAsync` — the most robust workaround but requires maintaining a copy

**Key quote** (library maintainer @chrissainty): *"Changing the library to make all validation calls synchronous could have unwanted side effects... However, having a good solution for async validation is a must. I think I will bring this up with the Blazor team directly."*

**Key quote** (community member): *"No shade intended on this library or others like it — it's a core issue for the Blazor team to solve. I'm just a little disappointed async code is so mature at this point yet async EditContext isn't a thing."*

---

## 6. Async Validation in Other Frameworks

### 6.1 Angular

- **First-class support** for async validators via `AsyncValidatorFn` returning `Promise` or `Observable`
- Async validators run **only after synchronous validators pass** (optimization: skip expensive async checks if sync validation already fails)
- Built-in `PENDING` state on form controls — `control.status === 'PENDING'`
- Debouncing built into reactive forms via `updateOn: 'blur'`
- Pattern: sync validators first, then async; cancel on value change via Observable unsubscription

```typescript
export function usernameValidator(userService: UserService): AsyncValidatorFn {
  return (control: AbstractControl): Observable<ValidationErrors | null> => {
    return userService.checkUsername(control.value).pipe(
      map(isAvailable => (isAvailable ? null : { usernameTaken: true })),
      catchError(() => of(null)),  // Prevents stuck PENDING state
      first()                      // Ensures Observable completes
    );
  };
}
```

**Status lifecycle**: `VALID`/`INVALID` → value changes → sync validators run → if pass → `PENDING` → async validator resolves → `VALID`/`INVALID`

**CSS classes** (automatically applied):
- `ng-valid` / `ng-invalid` — validation result
- `ng-pending` — async validation in progress
- `ng-dirty` / `ng-pristine` — modified state
- `ng-touched` / `ng-untouched` — interaction state

**`statusChanges` Observable**: Emits `'VALID'`, `'INVALID'`, `'PENDING'`, or `'DISABLED'` on every transition — allows reactive UI binding.

**Known pitfalls**:
- Validators returning Observables that never complete → form stuck in `PENDING` forever
- Parent `FormGroup` status depends on children — can lag behind child updates
- Excessive API calls without `updateOn: 'blur'` or debouncing

### 6.2 React (react-hook-form / Formik)

- No built-in framework-level async validation; handled by form libraries
- **react-hook-form**: `validate` can be an async function; provides `formState.isValidating` state
- **Formik**: Supports async `validate` functions at form and field level
- **Zod/Yup resolvers**: Can integrate async schema validation
- Common pattern: trigger async on blur, debounce during typing

**Debounce + cancellation pattern** (community best practice):
```jsx
function useDebouncedAsyncValidation() {
  const validateCounter = useRef(0);
  function getValidator(asyncValidator, delay = 400) {
    const debouncedValidate = debounce(async (value, resolve, currentCount) => {
      const isValid = await asyncValidator(value);
      if (validateCounter.current === currentCount) {
        resolve(isValid || "Validation failed");
      }
    }, delay);
    return (value) => {
      validateCounter.current += 1;
      const current = validateCounter.current;
      return new Promise((resolve) => debouncedValidate(value, resolve, current));
    };
  }
  return { getValidator };
}
```

**Key features:**
- `isValidating` flag exposed on form state — consumers can show spinners
- Counter/AbortController pattern for cancelling stale validations
- No built-in debounce — must be implemented manually
- Async validators in schema resolvers (Zod/Yup) aren't debounced by default

### 6.3 Django

- Validation is primarily synchronous (forms/serializers)
- Async scenarios handled via AJAX calls to REST endpoints from the frontend
- Django 4+ supports async views for non-blocking checks, but form machinery remains sync

### 6.4 Spring Boot

- Backend-only validation via JSR-380 (Bean Validation) — synchronous
- Frontend sends AJAX validation requests to REST endpoints
- No framework-level async validator concept

### 6.5 Key Takeaways from Other Frameworks

| Framework | Async Validator Support | Pending State | Debouncing | Cancel on Change |
|---|---|---|---|---|
| Angular | ✅ First-class | ✅ Built-in `PENDING` | ✅ Built-in | ✅ Observable cancel |
| React (react-hook-form) | ✅ Via library | ✅ `isValidating` | Manual | Manual |
| Django | ❌ Server-side | ❌ N/A | ❌ N/A | ❌ N/A |
| Spring Boot | ❌ Server-side | ❌ N/A | ❌ N/A | ❌ N/A |
| **Blazor (current)** | ❌ No | ❌ No | ❌ No | ❌ No |

Angular's approach is the gold standard: sync-first execution, built-in pending state with CSS classes, observable-based cancellation. React's react-hook-form provides a lighter-weight but effective model with `isValidating` and manual debounce/cancel patterns.

**Design lessons for Blazor:**
- Run sync validators first, async only if sync passes (Angular pattern)
- Provide built-in pending state at both field and form level
- Provide CSS class for pending state (equivalent to Angular's `ng-pending`)
- Cancellation must be first-class (user edits field → cancel previous async validation for that field)
- Debouncing is a nice-to-have at framework level, must-have at library level

---

## 7. Existing Workarounds & Third-Party Libraries

### 7.1 Manual Workaround (from @SteveSandersonMS)

```razor
@code {
    bool checkingUsername;
    ValidationMessageStore extraValidationMessages;

    async Task TryRegister()
    {
        checkingUsername = true;
        try
        {
            if (await IsUsernameAvailable(user.Username!))
                NavManager.NavigateTo("done");
            else
                extraValidationMessages.Add(() => user.Username!, "Name already taken.");
        }
        finally { checkingUsername = false; }
    }
}
```

Drawbacks: lots of boilerplate, no integration with standard validation pipeline, no pending state.

### 7.2 Custom `OnValidationRequested` Handler

```csharp
editContext.OnValidationRequested += ValidateModelAsync;

private async void ValidateModelAsync(object sender, ValidationRequestedEventArgs e)
{
    messageStore.Clear();
    if (!await IsEmailUnique(Model.Email))
        messageStore.Add(() => Model.Email, "Email already in use.");
    editContext.NotifyValidationStateChanged();
}
```

**Critical problem**: `async void` — the form submits *before* the async handler completes. The `EditContext.Validate()` call returns immediately after invoking the event; it doesn't wait for async handlers.

### 7.3 Blazored.FluentValidation

- Hooks into `EditContext` via extension methods
- Uses `ValidateAsync()` from FluentValidation
- **Premature submission bug**: `EditForm` calls sync `Validate()` → fires sync `OnValidationRequested` → handler is `async void`, calls `validator.ValidateAsync()` → form submits *before* validation completes ([Blazored/FluentValidation#38](https://github.com/Blazored/FluentValidation/issues/38))
- FluentValidation docs explicitly warn: *"You should not use asynchronous rules when using automatic validation with ASP.NET as ASP.NET's validation pipeline is not asynchronous"*
- On Blazor WASM, `HttpClient` is async-only — there is literally no way to do synchronous HTTP calls for validation
- Community workarounds: sync-over-async (`Task.Run().GetAwaiter().GetResult()`), fake validation messages, forking `EditForm`, moving validation to server

### 7.4 Blazilla

- Built for async FluentValidation integration
- Has "AsyncMode" to ensure async rules are properly awaited
- Prevents form submission while async checks are pending
- Replaces `EditForm` with its own component to control the submission lifecycle
- Demonstrates the approach works but requires replacing framework components

### 7.5 Blazorise Validation

- Per-field async validators with debouncing
- Custom rendering of feedback including spinners for pending state
- Separate from standard Blazor forms infrastructure

### 7.6 Server-Side Validation Pattern

For cases where client async validation is impractical, some teams:
- Submit the form to an API endpoint
- Run async validation server-side (including FluentValidation `MustAsync` rules)
- Return `ValidationProblemDetails` on failure
- Display errors via a custom `ServerSideValidation` component

This works but splits validation logic between client and server, requiring two validation code paths.

### 7.7 Double-Submit Prevention

Common workaround for the "form submits before validation completes" problem:
```csharp
bool isSubmitting = false;
async Task HandleValidSubmit()
{
    if (isSubmitting) return;
    isSubmitting = true;
    try { await MyAsyncWork(); }
    finally { isSubmitting = false; }
}
```
```razor
<button type="submit" disabled="@isSubmitting">Submit</button>
```

This prevents double-submit but doesn't solve the core problem of async validation before submission.

---

## 8. UI Patterns for Pending Validation

### 8.1 Field-Level Indicators

- Spinner or "Checking…" text next to the field while async validation runs
- Replace with ✓ (valid) or ✗ (invalid) when complete
- Use per-field state tracking: `isPending`, `isValid`, `errorMessage`

### 8.2 Form-Level Indicators

- Disable submit button while any field has pending validation
- Show "Validating…" overlay or message
- `EditForm` could expose `IsValidating` property/event

### 8.3 CSS Class Integration

Current `FieldCssClassProvider` returns `valid`/`invalid`/`modified`. Could add:
- `"pending"` — async validation in progress for this field
- `"modified pending"` — field modified AND validation pending

The `FieldCssClassProvider` is already customizable — developers can subclass it and return any CSS classes:
```csharp
public class CustomFieldCssClassProvider : FieldCssClassProvider
{
    public override string GetFieldCssClass(EditContext editContext, in FieldIdentifier fieldIdentifier)
    {
        var isModified = editContext.IsModified(fieldIdentifier);
        var isValid = !editContext.GetValidationMessages(fieldIdentifier).Any();
        // Could add: var isPending = editContext.HasPendingValidation(fieldIdentifier);
        if (!isModified) return "";
        return isValid ? "is-valid" : "is-invalid";
    }
}
```

**Integration with CSS frameworks**: Bootstrap uses `is-valid`/`is-invalid` (not Blazor's defaults). The `FieldCssClassProvider` is the right place to map Blazor states to framework-specific classes. Adding a `pending` state will work naturally with this customization point.

**Known limitation** ([#30496](https://github.com/dotnet/aspnetcore/issues/30496)): Blazor doesn't bulk-mark all fields as modified on submit, so CSS classes only appear on individually-interacted fields unless custom logic is added.

### 8.4 Angular's `PENDING` State (Reference Model)

Angular form controls have a `status` property with values: `VALID`, `INVALID`, `PENDING`, `DISABLED`. When an async validator is running, the control enters `PENDING` state and CSS class `ng-pending` is applied.

**Status lifecycle**:
```
Value changes → sync validators run → VALID/INVALID
  → if sync passes → async validators start → PENDING
    → async resolves → VALID/INVALID
```

**`statusChanges` Observable** emits on every transition — allows reactive UI binding for spinners, disabling, etc.

**Key Angular design decisions relevant to Blazor:**
- Async only runs after sync passes — avoids wasted async calls
- Both per-field (`FormControl.pending`) and per-form (`FormGroup.pending`) status
- Parent form status is derived from children — form is `PENDING` if any child is `PENDING`
- CSS class applied automatically — no manual tracking needed

### 8.5 Recommended Pattern for Blazor

1. Add `HasPendingValidation` to `EditContext` (per-field via `FieldIdentifier` and whole-form)
2. Add `"pending"` CSS class via `FieldCssClassProvider`
3. Expose `IsValidating` on `EditForm` or via cascading parameter
4. Fire `OnValidationStateChanged` when pending state changes (reuses existing event)

---

## 9. Key Technical Challenges

### 9.1 Synchronous Event Model

`EditContext.OnValidationRequested` is `EventHandler<ValidationRequestedEventArgs>` (returns `void`). Cannot be made async without:
- **Option A**: Add new `OnValidationRequestedAsync` event (`Func<object, ValidationRequestedEventArgs, Task>`)
- **Option B**: Replace the event model entirely (breaking change)
- **Option C**: Use `ValidateAsync()` method that bypasses events and calls validation directly

### 9.2 Sync `EditContext.Validate()` Must Remain

For backward compatibility, `Validate()` must continue to work synchronously. New `ValidateAsync()` is additive.

### 9.3 `GetValidationResult()` Is Sync in BCL

`ValidationAttribute.GetValidationResult()` is synchronous. Until the BCL adds `GetValidationResultAsync()` (Phase 1 of the proposal), the `Microsoft.Extensions.Validation` package's async orchestration still calls sync attributes. However:
- The *orchestration* being async is still valuable (non-blocking traversal)
- Custom `AsyncValidationAttribute` subclasses will work once BCL support lands
- Existing sync attributes work unchanged via `ValueTask` wrapping

### 9.4 Race Conditions & Cancellation

- User edits field → async validation starts → user edits again → must cancel previous validation
- Multiple fields validating concurrently — need per-field task tracking
- Form submit while field validation is in progress — wait or cancel?

**Proven pattern from React/Angular:**
```csharp
// Version counter per field — only apply result if version matches
private int emailCheckVersion = 0;
private async Task CheckEmailUniqueAsync(string email)
{
    int version = ++emailCheckVersion;
    bool isUnique = await UserService.IsEmailUniqueAsync(email);
    if (version == emailCheckVersion) // Only apply if still current
    {
        // Update validation messages
    }
}
```

**CancellationToken approach** (more idiomatic for .NET):
- Store a `CancellationTokenSource` per field
- Cancel previous CTS when a new validation starts
- Pass token to async validation method

### 9.5 Blazor WASM Constraints

On Blazor WebAssembly, `HttpClient` is async-only — there is no synchronous HTTP API available. This means:
- Sync-over-async workarounds (`Task.Run().GetAwaiter().GetResult()`) will deadlock
- Any validation that needs a server call *must* be async
- This makes async validation support not just a convenience but a hard requirement for WASM apps

### 9.6 Rendering Behavior

- Blazor re-renders on `OnValidationStateChanged` — need to fire this when pending state changes too
- `StateHasChanged()` must be called on sync context (Blazor dispatcher) after async work completes
- `InputBase` already subscribes to `OnValidationStateChanged` — adding pending state should work

### 9.7 Static SSR Forms

- Enhanced forms use HTTP POST, response is a full page render
- Async validation could happen server-side during form processing
- No WebSocket — can't show progressive pending UI
- Must validate fully before response

---

## 10. Summary of Integration Points

### What Needs to Change

| Component | Change | Priority |
|---|---|---|
| `EditContext` | Add `ValidateAsync()` method | **P0** |
| `EditContext` | Add async validation event or callback mechanism | **P0** |
| `EditContext` | Add `HasPendingValidation` (per-field + whole-form) | **P0** |
| `EditForm` | Call `ValidateAsync()` in `HandleSubmitAsync()` | **P0** |
| `EditContextDataAnnotationsExtensions` | Remove async throw guard, properly await `ValidateAsync()` | **P0** |
| `FieldCssClassProvider` | Support `"pending"` CSS class | **P1** |
| `FieldState` | Track pending validation tasks | **P1** |
| `EditForm` / cascading | Expose `IsValidating` state | **P1** |
| `ValidationMessageStore` | No changes needed (already supports incremental add/clear) | N/A |
| `InputBase` | No changes needed (already re-renders on state change) | N/A |
| `ValidationMessage` / `ValidationSummary` | No changes needed (already reactive) | N/A |

### What Already Works

- `Microsoft.Extensions.Validation` has async `ValidateAsync()` infrastructure
- `ValidateContext.OnValidationError` streams errors in real-time
- `EditForm.HandleSubmitAsync()` is already async
- `InputBase`, `ValidationMessage`, `ValidationSummary` react to `OnValidationStateChanged`
- `ValidationMessageStore` supports incremental add/clear

### What's Blocked on BCL Changes

- `AsyncValidationAttribute` (new BCL type)
- `Validator.TryValidateObjectAsync()` (new BCL API)
- `IAsyncValidatableObject` (new BCL interface)
- `ValidationAttribute.GetValidationResultAsync()` (new BCL method)

### What Can Be Done Without BCL Changes

- `EditContext.ValidateAsync()` — new method
- Async event model for validation requests
- Pending state tracking and CSS classes
- `EditForm` calling `ValidateAsync()` instead of `Validate()`
- Removing the "async not supported" throw guard in `EditContextDataAnnotationsExtensions`
- UI components for pending indicators

---

## Source References

### Local Code
- `src/Components/Forms/src/EditContext.cs` — Core validation orchestrator
- `src/Components/Forms/src/EditContextDataAnnotationsExtensions.cs` — DataAnnotations wiring (contains async throw guard)
- `src/Components/Web/src/Forms/EditForm.cs` — Form component (contains "ValidateAsync later" comment)
- `src/Components/Web/src/Forms/InputBase.cs` — Input base class
- `src/Components/Web/src/Forms/FieldCssClassProvider.cs` — CSS class logic
- `src/Validation/src/` — New async validation infrastructure

### GitHub Issues & PRs
- [#64892](https://github.com/dotnet/aspnetcore/issues/64892) — **Blazor Form Validation Enhancements** epic (11.0-preview4, @oroztocil) — the tracking epic for our work
- [#7680](https://github.com/dotnet/aspnetcore/issues/7680) — Async validation for forms (original tracking issue, open since 2019, 11.0-preview4)
- [#31905](https://github.com/dotnet/aspnetcore/issues/31905) — **"Please reconsider allowing async model validation"** by FluentValidation author (227👍, .NET 11 Planning)
- [#7614](https://github.com/dotnet/aspnetcore/pull/7614) — Original PR adding Blazor forms (mentions async as future work)
- [#40244](https://github.com/dotnet/aspnetcore/issues/40244) — Async validation request (closed, duped to #7680)
- [#51501](https://github.com/dotnet/aspnetcore/issues/51501) — ValidateAsync proposal (closed, duped to #7680)
- [#51040](https://github.com/dotnet/aspnetcore/issues/51040) — Client validation without circuit (11.0-preview4, @oroztocil)
- [#12158](https://github.com/dotnet/aspnetcore/issues/12158) — Data annotations localization (11.0-preview3, @oroztocil)
- [#64609](https://github.com/dotnet/aspnetcore/issues/64609) — Async validation in minimal APIs (.NET 11, assigned @halter73)
- [#30496](https://github.com/dotnet/aspnetcore/issues/30496) — Custom validation class CSS not working as expected
- [dotnet/designs#363](https://github.com/dotnet/designs/pull/363) — BCL async validation design proposal by @halter73
- [dotnet/runtime#121536](https://github.com/dotnet/runtime/issues/121536) — Runtime-side tracking for async DataAnnotations

### External Libraries & Community
- [Blazored/FluentValidation#38](https://github.com/Blazored/FluentValidation/issues/38) — Async validator not working (canonical bug report)
- [Blazilla](https://github.com/loresoft/Blazilla/) — FluentValidation async integration for Blazor (works around EditForm limitations)
- [Blazored.FluentValidation](https://github.com/Blazored/FluentValidation) — FluentValidation Blazor integration
- [Blazorise Validation](https://blazorise.com/blog/handling-complex-forms-with-validation-and-dynamic-rules) — Async validation with debouncing

### Framework References
- [Angular Async Validators](https://angular.dev/guide/forms/signals/async-operations) — First-class async with PENDING state
- [Angular Form Validation Docs](https://angular.dev/guide/forms/form-validation) — ng-pending CSS class, statusChanges Observable
- [react-hook-form async patterns](https://github.com/orgs/react-hook-form/discussions/9005) — isValidating, debounce, cancel patterns
- [react-hook-form debounce issue](https://github.com/react-hook-form/react-hook-form/issues/40) — Community debounce patterns

### Blog Posts & Tutorials
- [Blazor Form UX Patterns](https://www.dotnet-guide.com/articles/blazor-form-ux-patterns/) — Async validation, dirty state, inline errors
- [Blazilla blog post](https://loresoft.com/post/blazilla-fluentvalidation-blazor/) — AsyncMode for FluentValidation
- [Preventing double form submission](https://www.meziantou.net/preventing-double-form-submission-in-a-blazor-application.htm) — isSubmitting pattern
- [Custom form validation in Blazor](https://www.bytefish.de/blog/custom_form_validation_blazor.html) — Manual async validation
- [Blazor University: Custom Validation](https://blazor-university.com/forms/writing-custom-validation/) — Manual validation patterns
- [Microsoft Docs: Blazor Forms Validation](https://learn.microsoft.com/en-us/aspnet/core/blazor/forms/validation) — Official documentation
