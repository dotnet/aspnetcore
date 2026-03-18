# Async Validation Support for Blazor Forms

## Summary

This document proposes adding async validation support to Blazor's form validation infrastructure (`EditContext`, `EditForm`, `DataAnnotationsValidator`, and related components) to address the inability to perform I/O-bound validation operations — such as database uniqueness checks, remote API calls, or service-based business rule validation — within the standard Blazor form validation pipeline.

Today, the entire Blazor form validation pipeline is synchronous. `EditContext.Validate()` fires a synchronous event and returns `bool`. Developers who need async validation are forced to block on async operations using `.Result` or `.GetAwaiter().GetResult()`, which is incompatible with Blazor WebAssembly and causes deadlocks on Blazor Server. Third-party libraries like FluentValidation — which has first-class async support — cannot fully integrate with Blazor's `EditContext` because the event model is synchronous.

This proposal adds `EditContext.ValidateAsync()`, an async event model for validation requests, per-field async validation task tracking with pending state and cancellation, and UI feedback mechanisms including CSS classes and an optional indicator component — enabling async validation to work end-to-end across all Blazor rendering modes.

## Goals

- **Enable async validation on form submit.** Developers need forms that await async validators (e.g., server-side uniqueness checks) before invoking `OnValidSubmit` or `OnInvalidSubmit`. Today, forms either submit before async validation completes or require sync-over-async blocking that deadlocks.

- **Enable async validation on field change.** Developers need individual fields to validate asynchronously as the user interacts with them (on change or blur), with errors appearing inline via `<ValidationMessage>` — the same way sync validation works today.

- **Expose pending and cancelled validation state.** While async validation is in progress, developers need to query pending state per-field and per-form to show spinners, "Checking…" text, or disable submit buttons. When async validation fails due to infrastructure errors (network failure, timeout, unhandled exception in the validator), the field should enter a "cancelled" state distinct from both "valid" and "invalid" — enabling UI like "Validation failed, please try again." Both states must trigger UI re-renders automatically.

- **Cancel stale validations.** When a user edits a field while its async validation is in progress, the previous validation must be cancelled (or its result discarded) so stale results never appear in the UI.

- **Maintain full backward compatibility.** Existing applications using sync-only validators must continue to work without any runtime regressions or performance overhead. The sync `Validate()` method will either be marked `[Obsolete]` (Alternative A) or gain a `syncOnly` parameter (Alternative B) — see Scenarios 8 and 9.

- **Enable third-party validator integration.** Library authors (e.g., FluentValidation) need an async-capable extension point to hook into the validation pipeline without forking `EditForm` or resorting to `async void` event handlers.

- **Integrate with Microsoft.Extensions.Validation.** The existing `DataAnnotationsValidator` should use the async-first `Microsoft.Extensions.Validation` pipeline (which already exists but currently throws if it doesn't complete synchronously) without blocking or throwing.

## Non-goals

- **BCL-level async validation APIs.** Adding `AsyncValidationAttribute`, `Validator.TryValidateObjectAsync()`, or `IAsyncValidatableObject` to `System.ComponentModel.DataAnnotations` is tracked separately ([dotnet/runtime#121536](https://github.com/dotnet/runtime/issues/121536), [dotnet/designs#363](https://github.com/dotnet/designs/pull/363)). This spec focuses on the Blazor-side changes that can proceed independently. When BCL async APIs land, they will flow through naturally.

- **Parallel execution strategy for validators.** Whether async validators on the same property run sequentially or concurrently, and how error short-circuiting works, are validation infrastructure concerns — not Blazor form concerns. This spec treats the validation engine as a black box that returns results.

- **Incremental error reporting during form-submit validation.** The built-in `DataAnnotationsValidator` will not stream errors progressively as each field's async validation completes during form submission. Instead, all validation results are batched and reported together once the entire validation pass finishes. Streaming errors one-by-one causes layout shifts and unpredictable UX as the error list grows during validation. Third-party validator components are free to implement incremental reporting via the `ValidationMessageStore` and `NotifyValidationStateChanged()` APIs if they choose to.

- **Built-in debouncing for async field validation.** Blazor's built-in input components bind to `onchange` (which fires on blur), not `oninput` (every keystroke). Since async validation already triggers on blur by default, debouncing is unnecessary for the common case. Using async validators with `oninput` binding is not recommended. Debouncing may be reconsidered as part of a future general event debounce feature for Blazor.

## Proposed solution

The solution adds async validation capabilities to Blazor forms through two complementary mechanisms on `EditContext`:

1. **`OnValidationRequestedAsync`** — a new async event that is the counterpart to the existing sync `OnValidationRequested`. Validator components subscribe to this event to run async validation on form submit. `EditContext.ValidateAsync()` invokes and awaits all handlers.

2. **`AddValidationTask(FieldIdentifier, Task)`** — a method for tracking in-flight async validation tasks per field. This enables pending state queries (`IsValidationPending`), automatic cancellation when a field is re-edited, and automatic cancellation on form submit (where full-form validation supersedes field-level tasks).

`EditForm.HandleSubmitAsync()` is updated to call `ValidateAsync()` instead of `Validate()`. The existing sync `Validate()` will either be marked `[Obsolete]` or gain an optional `syncOnly` parameter — two alternative designs are presented in Scenarios 8 and 9.

UI feedback is provided through a `"pending"` CSS class on `InputBase` (via `FieldCssClassProvider`) and a new `ValidationPendingIndicator` component.

### Scenario 1: Validate form with async validators on submit

A form with an async validator (e.g., uniqueness check) awaits all validation before invoking `OnValidSubmit`.

```razor
<EditForm Model="@user" OnValidSubmit="HandleValidSubmit">
    <DataAnnotationsValidator />

    <InputText @bind-Value="user.Username" />
    <ValidationMessage For="() => user.Username" />

    <button type="submit">Register</button>
</EditForm>

@code {
    private User user = new();

    private async Task HandleValidSubmit(EditContext context)
    {
        // This only fires after ALL validation (including async) has passed
        await UserService.CreateUserAsync(user);
    }
}
```

No changes to the developer's form code are needed — `EditForm` internally calls `ValidateAsync()` instead of `Validate()`.

### Scenario 2: Validate form with async validators explicitly

A developer using `OnSubmit` can await async validation explicitly.

```razor
<EditForm Model="@user" OnSubmit="HandleSubmit">
    <DataAnnotationsValidator />
    <!-- ... -->
</EditForm>

@code {
    private async Task HandleSubmit(EditContext context)
    {
        var isValid = await context.ValidateAsync();
        if (isValid)
        {
            await UserService.CreateUserAsync(user);
        }
    }
}
```

`ValidateAsync()` invokes both sync `OnValidationRequested` handlers and async `OnValidationRequestedAsync` handlers, then returns `!GetValidationMessages().Any()`.

### Scenario 3: Validate individual form field with async validators

A field with an async validator shows inline errors as the user interacts with it. Sync validators run immediately; async validators start afterward if sync validators pass.

```razor
<EditForm Model="@registration">
    <DataAnnotationsValidator />

    <InputText @bind-Value="registration.Email" />
    <ValidationMessage For="() => registration.Email" />

    <InputText @bind-Value="registration.Username" />
    <ValidationMessage For="() => registration.Username" />

    <button type="submit">Register</button>
</EditForm>
```

The async validation is defined on the model (once BCL async attribute support lands):

```csharp
public class Registration
{
    [Required]
    [UniqueEmail]    // async validator
    public string Email { get; set; } = "";

    [Required]
    [MinLength(3)]
    [UniqueUsername]  // async validator
    public string Username { get; set; } = "";
}
```

No manual wiring is needed — `DataAnnotationsValidator` handles the async validation lifecycle automatically.

> **TODO:** When multiple fields trigger async validation concurrently, should they run in parallel or be queued?

### Scenario 4: Display async validation state declaratively

Developers need a declarative way to show pending and cancelled validation feedback next to a field — without writing manual conditional rendering logic. The component should handle subscribing to state changes and rendering the appropriate content automatically.

The proposed approach extends `ValidationMessage` with optional `PendingContent` and `CancelledContent` render fragments. When async validation is pending, `PendingContent` is rendered instead of error messages. When validation has failed (network error, timeout), `CancelledContent` is rendered. When validation completes normally, the existing error message rendering is unchanged.

```razor
<InputText @bind-Value="registration.Username" />
<ValidationMessage For="() => registration.Username">
    <PendingContent>
        <span class="spinner">Checking availability…</span>
    </PendingContent>
    <CancelledContent>
        <span class="text-warning">Validation failed. Please try again.</span>
    </CancelledContent>
</ValidationMessage>
```

When the render fragments are not provided, `ValidationMessage` behaves exactly as it does today — it only renders error messages. This makes the change fully backward compatible.

> **TODO:** Alternative approaches to consider: (1) A separate dedicated `ValidationPendingIndicator` component instead of extending `ValidationMessage`. This provides cleaner separation of concerns and independent placement control, but requires two components per field. (2) Not providing any built-in component and leaving this to developers using the programmatic `IsValidationPending()` / `IsValidationCancelled()` APIs (Scenario 5).

### Scenario 5: Check async validation state programmatically

For cases where the declarative approach (Scenario 4) is insufficient — such as disabling the submit button, showing a form-level indicator, or implementing custom validation UX — developers can query pending and cancelled state directly on `EditContext`.

```razor
<EditForm Model="@registration">
    <DataAnnotationsValidator />

    <div class="form-group">
        <InputText @bind-Value="registration.Username" />

        @if (editContext.IsValidationPending(editContext.Field(nameof(registration.Username))))
        {
            <span class="spinner">Checking availability…</span>
        }
        else if (editContext.IsValidationCancelled(editContext.Field(nameof(registration.Username))))
        {
            <span class="text-warning">Validation failed. Please try again.</span>
        }
        else
        {
            <ValidationMessage For="() => registration.Username" />
        }
    </div>

    <button type="submit"
            disabled="@editContext.IsValidationPending()">
        Register
    </button>
</EditForm>
```

`IsValidationPending(FieldIdentifier)` returns `true` while a tracked async task is running for that field. `IsValidationPending()` (no args) returns `true` if any field has a pending task.

`IsValidationCancelled(FieldIdentifier)` returns `true` when an async validation task for the field completed with an exception (network error, timeout, etc.) — not when it was cancelled by the user re-editing the field. The cancelled state is cleared when the field is edited again (triggering a new validation attempt).

> **TODO:** The name "cancelled" may be confusing since this state is not about user-initiated cancellation (which is silent) but about validation infrastructure failures. Naming alternatives to consider: "failed" (`IsValidationFailed`), "faulted" (`IsValidationFaulted`, mirrors `Task.IsFaulted`), "errored" (`IsValidationErrored`), "inconclusive" (`IsValidationInconclusive`). Resolve naming before API review.

### Scenario 6: Style input forms based on async validation state

`InputBase` automatically applies `"pending"` or `"cancelled"` CSS classes based on async validation state, using the existing `FieldCssClassProvider` mechanism.

```css
input.pending {
    border-color: orange;
    background-image: url('/spinner.svg');
    background-repeat: no-repeat;
    background-position: right 8px center;
}

input.cancelled {
    border-color: #cc7700;
}
}

input.valid { border-color: green; }
input.invalid { border-color: red; }
```

Custom `FieldCssClassProvider` subclasses can map pending and cancelled states to any CSS class:

```csharp
public class BootstrapFieldCssClassProvider : FieldCssClassProvider
{
    public override string GetFieldCssClass(EditContext editContext, in FieldIdentifier fieldIdentifier)
    {
        if (editContext.IsValidationPending(fieldIdentifier))
            return "is-validating";

        if (editContext.IsValidationCancelled(fieldIdentifier))
            return "is-warning";

        var isValid = !editContext.GetValidationMessages(fieldIdentifier).Any();
        if (!editContext.IsModified(fieldIdentifier))
            return "";

        return isValid ? "is-valid" : "is-invalid";
    }
}
```

> **TODO:** Should we support a form-level CSS class (e.g., `<form class="validating">`) when any field has pending validation?

### Scenario 7: Cancellation of stale validations vs validation failures

There are two distinct situations where an async validation task does not complete normally:

1. **User-initiated cancellation** — the user edits the field again while validation is running. The previous task is cancelled via `CancellationToken` (`OperationCanceledException`). The result is silently discarded and a new validation starts. The field does **not** enter "cancelled" state — it transitions back to "pending" with the new validation.

2. **Validation failure** — the async validator throws a non-cancellation exception (network error, timeout, unhandled error). The field enters "cancelled" state, which is queryable via `IsValidationCancelled()` and reflected in CSS classes. The cancelled state is cleared when the field is edited again.

From the form developer's perspective, both are transparent:

```razor
<!-- No special handling needed -->
<InputText @bind-Value="user.Username" />
<ValidationMessage For="() => user.Username" />
```

Async validator authors receive a `CancellationToken` that is cancelled when the field value changes:

```csharp
public class UniqueUsernameAttribute : AsyncValidationAttribute
{
    protected override async ValueTask<ValidationResult?> IsValidAsync(
        object? value, ValidationContext ctx, CancellationToken cancellationToken)
    {
        var username = value as string;
        if (string.IsNullOrEmpty(username)) return ValidationResult.Success;

        // The CancellationToken is cancelled if the user edits the field again
        var userService = ctx.GetRequiredService<IUserService>();
        var exists = await userService.UsernameExistsAsync(username, cancellationToken);

        return exists
            ? new ValidationResult("Username is taken.")
            : ValidationResult.Success;
    }
}
```

### Scenario 8: Retry validation for a specific field

When async validation for a field fails (enters "cancelled" state), the developer or the user needs a way to retry validation for that specific field without changing its value and without re-validating the entire form.

```razor
<div class="form-group">
    <InputText @bind-Value="registration.Username" />

    @if (editContext.IsValidationCancelled(editContext.Field(nameof(registration.Username))))
    {
        <span class="text-warning">
            Validation failed.
            <button @onclick="() => editContext.ValidateFieldAsync(editContext.Field(nameof(registration.Username)))">
                Retry
            </button>
        </span>
    }
</div>
```

`ValidateFieldAsync(FieldIdentifier)` triggers validation for a specific field — clears any cancelled state, runs sync validators, starts async validators, and registers the async task via `AddValidationTask`.

> **TODO:** Technically, developers could call `NotifyFieldChanged(fieldIdentifier)` to achieve a similar effect today, since it triggers `OnFieldChanged` which runs validation in `DataAnnotationsEventSubscriptions`. However, `NotifyFieldChanged` is semantically a *value change* notification — it marks the field as modified and signals that the value has changed, which is not true in a retry scenario. A dedicated `ValidateFieldAsync` provides the correct semantics: "re-validate this field's current value" without implying a value change.

### Scenario 9: Backward compatibility — sync-only forms unchanged

Existing forms with sync-only validators continue to work identically at runtime. `EditForm` internally uses `ValidateAsync()`, so no changes are needed in form markup or `OnValidSubmit`/`OnInvalidSubmit` handlers.

For the sync `Validate()` method, there are two alternative designs under consideration:

**Alternative A — Mark `Validate()` as `[Obsolete]`:**

```csharp
// Still compiles and runs, but produces warning CS0618:
var isValid = editContext.Validate();
// Warning: 'EditContext.Validate()' is obsolete. Use 'ValidateAsync()' instead.

// Recommended:
var isValid = await editContext.ValidateAsync();
```

**Alternative B — Add an optional `syncOnly` parameter:**

```csharp
// Default behavior unchanged — but throws at runtime if model has async validators:
var isValid = editContext.Validate();

// Explicitly opt in to sync-only validation — no throw even with async validators,
// async validators are simply skipped:
var isValid = editContext.Validate(syncOnly: true);

// Full validation including async:
var isValid = await editContext.ValidateAsync();
```

Alternative B preserves `Validate()` as a non-obsolete API for cases where a developer intentionally wants only sync validation (e.g., instant field-level checks without triggering async work). Alternative A provides a cleaner migration signal but removes the ability to explicitly request sync-only validation.

> **TODO:** Resolve which alternative to use for `Validate()` — Obsolete vs `syncOnly` parameter.

Forms that use `OnValidSubmit` / `OnInvalidSubmit` (without calling `Validate()` directly) require no changes under either alternative — `EditForm` handles the transition internally.

```razor
<!-- This continues to work exactly as before — no changes needed -->
<EditForm Model="@person" OnValidSubmit="Save">
    <DataAnnotationsValidator />

    <InputText @bind-Value="person.Name" />
    <ValidationMessage For="() => person.Name" />

    <button type="submit">Save</button>
</EditForm>

@code {
    public class Person
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "";
    }
}
```

### Scenario 10: Sync `Validate()` called with async validators

Under **Alternative A** (`[Obsolete]`), developers get a compiler warning when calling `Validate()` directly. At runtime, if the validation infrastructure encounters an async validator, it throws `InvalidOperationException`.

Under **Alternative B** (`syncOnly` parameter), the default `Validate()` call (without `syncOnly: true`) throws at runtime if async validators are present. With `syncOnly: true`, async validators are silently skipped and only sync results are returned.

In both alternatives, if the model has async validators and the developer has not opted into an async-aware path, the runtime throws:

```csharp
// Runtime throws if model has async validators:
// InvalidOperationException:
//   "This model contains async validators. Use EditContext.ValidateAsync() instead of Validate()."
```

> **TODO:** Resolve which alternative to use — this scenario's behavior depends on the Scenario 9 decision.

### Scenario 11: Third-party validator integration

Third-party validator components subscribe to `OnValidationRequestedAsync` for form-submit validation and use `AddValidationTask` for field-level async validation. The existing pattern of creating validator components extends naturally to async scenarios.

```csharp
public class FluentValidationValidator : ComponentBase, IDisposable
{
    [CascadingParameter] EditContext EditContext { get; set; }

    protected override void OnInitialized()
    {
        EditContext.OnValidationRequestedAsync += async (sender, args) =>
        {
            var validator = GetValidatorForModel(EditContext.Model);
            var result = await validator.ValidateAsync(EditContext.Model);
            // Populate ValidationMessageStore with results
        };
    }
}
```

From the form developer's perspective, third-party validators work just like `DataAnnotationsValidator`:

```razor
<EditForm Model="@user" OnValidSubmit="HandleSave">
    <FluentValidationValidator />

    <InputText @bind-Value="user.Email" />
    <ValidationMessage For="() => user.Email" />

    <button type="submit">Save</button>
</EditForm>
```

### Scenario 12: Integration with Microsoft.Extensions.Validation

`DataAnnotationsValidator` uses the `Microsoft.Extensions.Validation` async pipeline when available. The current async throw guard is removed.

```csharp
[ValidatableType]
public class Order
{
    [Required]
    public string CustomerName { get; set; } = "";

    [Required]
    public List<OrderItem> Items { get; set; } = new();
}

[ValidatableType]
public class OrderItem
{
    [Required]
    public string ProductName { get; set; } = "";

    [Range(1, 1000)]
    public int Quantity { get; set; }

    [UniqueProductInOrder]  // async validator
    public string ProductId { get; set; } = "";
}
```

```razor
<!-- Deep async validation of Order → Items[i] → ProductId works automatically -->
<EditForm Model="@order" OnValidSubmit="SubmitOrder">
    <DataAnnotationsValidator />

    <InputText @bind-Value="order.CustomerName" />
    <ValidationMessage For="() => order.CustomerName" />

    @for (int i = 0; i < order.Items.Count; i++)
    {
        var item = order.Items[i];
        <InputText @bind-Value="item.ProductId" />
        <ValidationMessage For="() => item.ProductId" />
    }

    <ValidationSummary />
    <button type="submit">Place Order</button>
</EditForm>
```

Both the legacy `System.ComponentModel.Validator` path and the `Microsoft.Extensions.Validation` path are retained for now.

> **TODO:** Should we unify on `Microsoft.Extensions.Validation` or keep the legacy `Validator` path?

> **TODO:** Should we use `ValidateContext.OnValidationError` for real-time error streaming?

### Scenario 13: Static SSR forms

In Static Server Rendering mode, async validation runs server-side during form POST processing. Since there is no persistent connection, validation must complete fully before the response is rendered — no progressive UI updates are possible.

```razor
<EditForm Model="@contact" FormName="contact" Enhance OnValidSubmit="HandleSubmit">
    <DataAnnotationsValidator />

    <InputText @bind-Value="contact.Email" />
    <ValidationMessage For="() => contact.Email" />

    <button type="submit">Send</button>
</EditForm>

@code {
    [SupplyParameterFromForm]
    private ContactForm contact { get; set; } = new();

    private async Task HandleSubmit()
    {
        await ContactService.SaveAsync(contact);
    }
}
```

> **TODO:** Are there any interactions between server-side async validation for SSR forms and client-side validation (#51040)?

> **TODO:** Consider streaming validation UX for SSR enhanced forms: instead of blocking the entire response on async validation, the server could stream an initial render with the form in "pending" state (spinners, disabled button), then stream a DOM update with the final validation result. This would give the user immediate visual feedback instead of a blank browser loading state. This would need to be opt-in (e.g., a parameter on `EditForm`) since it changes the rendering behavior and is only useful when async validators are present. Only applicable with `Enhance` (enhanced navigation).

## Assumptions

- `Validator.TryValidatePropertyAsync` does not exist yet in the BCL. The field-level async validation path depends on this future API ([dotnet/runtime#121536](https://github.com/dotnet/runtime/issues/121536)). Until it ships, a polyfill or alternative implementation will be needed.
- `EditContext` is accessed from a single synchronization context (Blazor's component rendering thread). No concurrent access safeguards are needed.
- The `Microsoft.Extensions.Validation` package will address its current `[Experimental]` status and runtime type discovery limitations independently of this work.
- Async validation attributes (e.g., `AsyncValidationAttribute`, `UniqueUsernameAttribute` in the examples) are aspirational — they depend on BCL changes. The Blazor infrastructure is designed to support them when they arrive.

## References

- [dotnet/aspnetcore#7680](https://github.com/dotnet/aspnetcore/issues/7680) — Blazor form async validation enhancements (original issue, 2019)
- [dotnet/aspnetcore#64892](https://github.com/dotnet/aspnetcore/issues/64892) — Blazor Form Validation Enhancements epic (11.0-preview4)
- [dotnet/aspnetcore#31905](https://github.com/dotnet/aspnetcore/issues/31905) — "Please reconsider allowing async model validation" by FluentValidation author (227👍)
- [dotnet/aspnetcore#51501](https://github.com/dotnet/aspnetcore/issues/51501) — ValidateAsync proposal
- [dotnet/aspnetcore#40244](https://github.com/dotnet/aspnetcore/issues/40244) — Async validation request with manual workaround by @SteveSandersonMS
- [dotnet/aspnetcore#64609](https://github.com/dotnet/aspnetcore/issues/64609) — Async validation in minimal APIs
- [dotnet/runtime#121536](https://github.com/dotnet/runtime/issues/121536) — Runtime async validation APIs
- [dotnet/designs#363](https://github.com/dotnet/designs/pull/363) — BCL async validation design proposal
- [Blazored/FluentValidation#38](https://github.com/Blazored/FluentValidation/issues/38) — Async validator not working (canonical bug report)
- [Blazilla](https://github.com/loresoft/Blazilla/) — Third-party FluentValidation async integration for Blazor
- [Angular Async Validators](https://angular.dev/guide/forms/form-validation) — Reference implementation with `ng-pending` CSS class
- [react-hook-form](https://github.com/orgs/react-hook-form/discussions/9005) — Async validation patterns with `isValidating` state
