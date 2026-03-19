# Async Validation Support for Blazor Forms

## Summary

This document proposes adding async validation support to Blazor's form validation infrastructure (`EditContext`, `EditForm`, `DataAnnotationsValidator`, and related components) to address the inability to perform I/O-bound validation operations — such as database uniqueness checks, remote API calls, or service-based business rule validation — within the standard Blazor form validation pipeline.

Today, the entire Blazor form validation pipeline is synchronous. `EditContext.Validate()` fires a synchronous event and returns `bool`. Developers who need async validation are forced to block on async operations using `.Result` or `.GetAwaiter().GetResult()`, which is incompatible with Blazor WebAssembly and causes deadlocks on Blazor Server. Third-party libraries like FluentValidation — which has first-class async support — cannot fully integrate with Blazor's `EditContext` because the event model is synchronous. Additionally, async validation APIs are being added to `System.ComponentModel.DataAnnotations` in the BCL ([dotnet/runtime#121536](https://github.com/dotnet/runtime/issues/121536)); the Blazor form infrastructure needs to be ready to support these when they land.

This proposal adds `EditContext.ValidateAsync()`, an async event model for validation requests, per-field async validation task tracking with pending state and cancellation, and UI feedback mechanisms including CSS classes and an optional indicator component.

## Goals

- **Enable async validation on form submit.** Developers need forms that await async validators (e.g., server-side uniqueness checks) before invoking `OnValidSubmit` or `OnInvalidSubmit`, for both simple models and complex object graphs with nested objects and collections. Today, forms either submit before async validation completes or require sync-over-async blocking that deadlocks.

- **Enable async validation on field change.** Developers need individual fields to validate asynchronously as the user interacts with them (on change or blur), with errors appearing inline via `<ValidationMessage>` — the same way sync validation works today.

- **Expose pending and cancelled validation state.** While async validation is in progress, developers need to query pending state per-field and per-form to show spinners, "Checking…" text, or disable submit buttons. When async validation fails due to infrastructure errors (network failure, timeout, unhandled exception in the validator), the field should enter a "cancelled" state distinct from both "valid" and "invalid" — enabling UI like "Validation failed, please try again." Both states must trigger UI re-renders automatically.

- **Cancel stale validations.** When a user edits a field while its async validation is in progress, the previous validation must be cancelled (or its result discarded) so that only the validation result for the current field value is ever displayed.

- **Maintain full backward compatibility.** Existing applications using sync-only validators must continue to work without any runtime regressions or noticeable performance overhead.

- **Improve third-party async validator integration.** Library authors or integrators (e.g., FluentValidation) need an async-capable extension point to hook into the validation pipeline without forking `EditForm` or resorting to `async void` event handlers.

- **Support all Blazor rendering modes.** Async validation must work across interactive Server, WebAssembly, and Auto rendering modes — where `EditForm.HandleSubmitAsync()` is already async and the feature works naturally — as well as Static SSR, where there is no persistent connection and validation must complete fully before the response is rendered.

## Non-goals

- **BCL-level async validation APIs.** Adding async validation APIs to `System.ComponentModel.DataAnnotations` is tracked separately ([dotnet/runtime#121536](https://github.com/dotnet/runtime/issues/121536), [dotnet/designs#363](https://github.com/dotnet/designs/pull/363)). This spec focuses on the Blazor-side changes that can proceed independently.

- **Parallel execution strategy for validators.** Whether async validators on the same property run sequentially or concurrently, and how error short-circuiting works, are validation infrastructure concerns — not Blazor form concerns. This spec treats the validation engine as a black box that returns results.

- **Incremental error reporting during form-submit validation.** The built-in `DataAnnotationsValidator` will not stream errors progressively as each field's async validation completes during form submission. Instead, all validation results are batched and reported together once the entire validation pass finishes. Streaming errors one-by-one causes layout shifts and unpredictable UX as the error list grows during validation. Third-party validator components are free to implement incremental reporting via the `ValidationMessageStore` and `NotifyValidationStateChanged()` APIs if they choose to.

- **Built-in debouncing for async field validation.** Blazor's built-in input components bind to `onchange` (which fires on blur), not `oninput` (every keystroke). Since async validation already triggers on blur by default, debouncing is unnecessary for the common case. Using async validators with `oninput` binding is not recommended. Developers who need debounced validation (e.g., with `oninput`) can implement the behavior by e.g. delaying the calls to `NotifyFieldChanged`. Built-in debouncing will be reconsidered as part of a future general event debounce feature for Blazor.

## Proposed solution

The solution adds async validation capabilities to Blazor forms through two complementary mechanisms on `EditContext`:

1. **`OnValidationRequestedAsync`** — a new async event that is the counterpart to the existing sync `OnValidationRequested`. Validator components subscribe to this event to run async validation on form submit. `EditContext.ValidateAsync()` invokes and awaits all handlers.

2. **`AddValidationTask(FieldIdentifier, Task)`** — a method for tracking in-flight async validation tasks per field. This enables pending state queries (`IsValidationPending`), automatic cancellation when a field is re-edited, and automatic cancellation on form submit (where full-form validation supersedes field-level tasks).

`EditForm.HandleSubmitAsync()` is updated to call `ValidateAsync()` instead of `Validate()`. The existing sync `Validate()` will either be marked `[Obsolete]` or gain an optional `syncOnly` parameter — two alternative designs are presented in Scenario 6.

UI feedback is provided through `"pending"` and `"cancelled"` CSS classes on `InputBase` (via `FieldCssClassProvider`) and optional render fragments on `ValidationMessage`.

### Scenario 1: Validate form with async validators on submit

A form with an async validator (e.g., uniqueness check) awaits all validation before invoking `OnValidSubmit`.

The following model is used throughout the scenarios in this spec:

```csharp
public class Registration
{
    [Required]
    [UniqueEmail]    // async validator
    public string Email { get; set; } = "";

    [Required]
    [UniqueUsername]  // async validator
    public string Username { get; set; } = "";
}
```

```razor
<EditForm Model="@registration" OnValidSubmit="HandleValidSubmit">
    <DataAnnotationsValidator />

    <InputText @bind-Value="registration.Username" />

    <button type="submit">Register</button>
    <ValidationSummary />
</EditForm>

@code {
    private Registration registration = new();

    private async Task HandleValidSubmit(EditContext context)
    {
        // This only fires after ALL validation (including async) has passed
        await UserService.CreateAsync(registration);
    }
}
```

No changes to the developer's form code are needed — `EditForm` internally calls `ValidateAsync()` instead of `Validate()`.

### Scenario 2: Validate form with async validators explicitly

A developer using `OnSubmit` can await async validation explicitly via `EditContext.ValidateAsync`.

```razor
<EditForm Model="@registration" OnSubmit="HandleSubmit">
    <DataAnnotationsValidator />
    <!-- ... -->
</EditForm>

@code {
    private async Task HandleSubmit(EditContext context)
    {
        var isValid = await context.ValidateAsync();
        if (isValid)
        {
            await UserService.CreateAsync(registration);
        }
    }
}
```

`ValidateAsync()` invokes both sync `OnValidationRequested` handlers and async `OnValidationRequestedAsync` handlers, then returns `!GetValidationMessages().Any()`.

### Scenario 3: Validate individual form field with async validators

A field with an async validator shows inline errors as the user interacts with it, with no additional code beyond declaring the validator attributes on the model.

```razor
<EditForm Model="@registration">
    <DataAnnotationsValidator />

    <InputText @bind-Value="registration.Email" />
    <ValidationMessage For="() => registration.Email" />
    ...
</EditForm>
```

No manual wiring is needed — `DataAnnotationsValidator` handles the async validation lifecycle automatically.

### Scenario 4: Validate complex form with async validators on submit

For forms with complex object graphs (nested objects, collections), async validation works automatically when the application uses `AddValidation()` in startup and `DataAnnotationsValidator` in the form. The `Microsoft.Extensions.Validation` infrastructure traverses the object graph asynchronously, validating nested properties including those with async validators.

```csharp
// Program.cs — enables Microsoft.Extensions.Validation for complex object graph traversal
builder.Services.AddValidation();
```

```csharp
[ValidatableType]
public class Order
{
    [Required]
    public string CustomerName { get; set; } = "";

    [Required]
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    [Required]
    public string ProductName { get; set; } = "";

    [UniqueProductInOrder]  // async validator checking server-side inventory
    public string ProductId { get; set; } = "";
}
```

```razor
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

Both the legacy `System.ComponentModel.Validator` path and the `Microsoft.Extensions.Validation` path are retained for now. If `AddValidation()` is not called, the validation falls back to the static `Validator` path, which only supports synchronous validation.

### Scenario 5: Submit form while per-field async validations are pending

When the user submits the form while per-field async validations are still in progress (e.g., a uniqueness check is running for the Username field), the pending field-level validations are cancelled because the form-submit validation supersedes them. The full-form validation via `ValidateAsync()` re-validates everything from scratch — there is no need to wait for the field-level tasks to finish first, and doing so would cause double work.

```
User blurs Username field → async field validation starts (pending)
User immediately clicks Submit:
  1. Pending Username validation is cancelled
  2. Full-form ValidateAsync() runs — re-validates all fields (including Username) from scratch
  3. OnValidSubmit or OnInvalidSubmit fires based on the full-form result
```

From the developer's perspective, no special handling is needed — the form code is identical to Scenario 1. The cancellation and re-validation happen automatically inside `EditContext.ValidateAsync()`.

### Scenario 6: Use existing sync-only forms without changes

Existing forms with sync-only validators continue to work identically at runtime. `EditForm` internally uses `ValidateAsync()`, which invokes both existing sync `OnValidationRequested` handlers and the new async `OnValidationRequestedAsync` handlers — so existing validator components continue to work without changes. No changes are needed in form markup or `OnValidSubmit`/`OnInvalidSubmit` handlers.

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

> **TODO:** Resolve which alternative to use for `Validate()` — Obsolete vs `syncOnly` parameter vs some other solution.

In both alternatives, if the model has async validators and the developer calls `Validate()` without opting into sync-only mode, the validation infrastructure throws at runtime:

```csharp
// Runtime throws if model has async validators:
// InvalidOperationException:
//   "This model contains async validators. Use EditContext.ValidateAsync() instead of Validate()."
```

This prevents silent data loss where async validators would be silently skipped.

### Scenario 7: Display async validation state declaratively

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

> **TODO:** `ValidationMessage` currently does not accept render fragments — it is a "leaf" component that renders a flat list of `<div class="validation-message">` elements. Adding `PendingContent` and `CancelledContent` render fragments changes it to a component that accepts child content, which could be a breaking change in the rendered DOM structure. This needs to be evaluated carefully during design.

> **TODO:** Alternative approaches to consider: (1) A separate dedicated `ValidationPendingIndicator` component instead of extending `ValidationMessage`. This avoids the DOM breaking change concern above and provides cleaner separation of concerns and independent placement control, but requires two components per field. (2) Not providing any built-in component and leaving this to developers using the programmatic `IsValidationPending()` / `IsValidationCancelled()` APIs (Scenario 8).

### Scenario 8: Check async validation state programmatically

For cases where the declarative approach (Scenario 7) is insufficient — such as disabling the submit button, showing a form-level indicator, or implementing custom validation UX — developers can query pending and cancelled state directly on `EditContext`.

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

`IsValidationCancelled(FieldIdentifier)` returns `true` when an async validation task for the field failed due to an infrastructure error — see Scenario 10 for the detailed distinction between user-initiated cancellation (silent) and validation failures (enters cancelled state).

> **TODO:** Discuss naming. The name "cancelled" may be confusing since this state is not about user-initiated cancellation (which is silent) but about validation infrastructure failures. Naming alternatives to consider: "failed" (`IsValidationFailed`), "faulted" (`IsValidationFaulted`, mirrors `Task.IsFaulted`), "errored" (`IsValidationErrored`), "inconclusive" (`IsValidationInconclusive`). Resolve naming before API review.

### Scenario 9: Style input forms based on async validation state

`InputBase` automatically applies `"pending"` or `"cancelled"` CSS classes based on async validation state, using the existing `FieldCssClassProvider` mechanism. Following the existing pattern of compound classes (`"modified valid"`, `"modified invalid"`), modified fields with async validation will get `"modified pending"` or `"modified cancelled"`.

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

### Scenario 10: Handle stale validations and validation failures

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

> **TODO:** The scenario above describes field-level validation failure. We need to decide what happens when an `OnValidationRequestedAsync` handler throws during form-submit validation (e.g., the validation infrastructure encounters a network error during full-form validation). Options: (1) the exception propagates out of `EditForm.HandleSubmitAsync` (neither `OnValidSubmit` nor `OnInvalidSubmit` fires), (2) the form enters a form-level "cancelled" state, or (3) the form is treated as invalid.

### Scenario 11: Retry validation for a specific field

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

`ValidateFieldAsync(FieldIdentifier)` is a new method proposed on `EditContext`. It triggers validation for a specific field — clears any cancelled state, runs validators, and registers the async task for tracking. The field transitions from "cancelled" → "pending" (while async validation runs) → "valid" or "invalid" (based on the validation result).

> **TODO:** Technically, developers could call `NotifyFieldChanged(fieldIdentifier)` to achieve a similar effect today, since it triggers `OnFieldChanged` which runs validation in `DataAnnotationsEventSubscriptions`. However, `NotifyFieldChanged` is semantically a *value change* notification — it marks the field as modified and signals that the value has changed, which is not true in a retry scenario. A dedicated `ValidateFieldAsync` provides the correct semantics: "re-validate this field's current value" without implying a value change.

### Scenario 12: Integrate third-party async validators

Third-party validator components subscribe to `OnValidationRequestedAsync` for form-submit validation and use `AddValidationTask` for field-level async validation. The existing pattern of creating validator components extends naturally to async scenarios.

```csharp
public class FluentValidationValidator : ComponentBase
{
    [CascadingParameter] EditContext EditContext { get; set; }

    protected override void OnInitialized()
    {
        // Form-submit: subscribe to the async validation event
        EditContext.OnValidationRequestedAsync += async (sender, args) =>
        {
            var validator = GetValidatorForModel(EditContext.Model);
            var result = await validator.ValidateAsync(EditContext.Model);
            // Populate ValidationMessageStore with results
        };

        // Per-field: subscribe to the existing sync field change event,
        // start async validation and register it for tracking/cancellation
        EditContext.OnFieldChanged += (sender, args) =>
        {
            var cts = new CancellationTokenSource();
            // Note: this is the component's own method, not EditContext.ValidateFieldAsync
            var task = RunFieldValidationAsync(args.FieldIdentifier, cts.Token);
            EditContext.AddValidationTask(args.FieldIdentifier, task, cts);
        };
    }
}
```

For form-submit validation, the component subscribes to `OnValidationRequestedAsync` — `EditContext.ValidateAsync()` awaits all handlers before determining validity. For per-field validation, the component subscribes to the existing sync `OnFieldChanged` event and registers async work via `AddValidationTask`, which enables pending state tracking and automatic cancellation when the field changes again.

From the form developer's perspective, third-party validators work just like `DataAnnotationsValidator`:

```razor
<EditForm Model="@user" OnValidSubmit="HandleSave">
    <FluentValidationValidator />

    <InputText @bind-Value="user.Email" />
    <ValidationMessage For="() => user.Email" />

    <button type="submit">Save</button>
</EditForm>
```

### Scenario 13: Validate forms with async validators in static SSR

In Static Server Rendering mode, async validation runs server-side during form POST processing. Since there is no persistent connection, validation must complete fully before the response is rendered — no progressive UI updates are possible. This applies to both enhanced (`Enhance`) and non-enhanced forms — in the non-enhanced case, `ValidateAsync` is awaited during POST processing and the complete result (with any validation errors) is rendered in the full-page response.

```razor
@* SSR-specific: FormName and SupplyParameterFromForm are required for static rendering *@
<EditForm Model="@contact" FormName="contact" Enhance OnValidSubmit="HandleSubmit">
    <DataAnnotationsValidator />

    <InputText @bind-Value="contact.Email" />
    <ValidationMessage For="() => contact.Email" />

    <button type="submit">Send</button>
</EditForm>

@code {
    [SupplyParameterFromForm]
    private ContactForm contact { get; set; } = new();

    // Async validation completes server-side before this handler is invoked
    private async Task HandleSubmit() => await ContactService.SaveAsync(contact);
}
```

> **TODO:** Are there any interactions between server-side async validation for SSR forms and client-side validation (#51040)?

> **TODO:** Consider streaming validation UX for SSR enhanced forms: instead of blocking the entire response on async validation, the server could stream an initial render with the form in "pending" state (spinners, disabled button), then stream a DOM update with the final validation result. This would give the user immediate visual feedback instead of a blank browser loading state. This would need to be opt-in (e.g., a parameter on `EditForm`) since it changes the rendering behavior and is only useful when async validators are present. Only applicable with `Enhance` (enhanced navigation).

## Assumptions

- `Validator.TryValidatePropertyAsync` and `Validator.TryValidateObjectAsync` do not exist yet in the BCL. The async validation path without `AddValidation` depends on this future API ([dotnet/runtime#121536](https://github.com/dotnet/runtime/issues/121536)). Until it ships, a polyfill or alternative implementation will be needed.
- `EditContext` is accessed from a single synchronization context (Blazor's component rendering thread). No concurrent access safeguards are needed.
- The order in which sync and async validators execute for a given field or model (e.g., whether sync validators run before async ones, or whether async validators are skipped when sync validators fail) is determined by the validation infrastructure (`Microsoft.Extensions.Validation`, `System.ComponentModel.DataAnnotations`), not by Blazor's form components. This spec does not prescribe validator execution order.

## References

- [dotnet/aspnetcore #64892](https://github.com/dotnet/aspnetcore/issues/64892) — Tracking epic for Blazor Form Validation Enhancements in .NET 11. This is the parent work item under which this feature is planned.
- [dotnet/aspnetcore #7680](https://github.com/dotnet/aspnetcore/issues/7680) — Original design sketch for async support in Blazor form validation. Contains the initial API shape (`AddValidationTask`, `HasPendingValidationTasks`, `ValidateAsync`) that informs this proposal.
- [dotnet/aspnetcore #31905](https://github.com/dotnet/aspnetcore/issues/31905) — Request from the FluentValidation author to allow async model validation in ASP.NET Core (227👍). Demonstrates the ecosystem demand and the dual sync/async code path maintenance burden that third-party libraries face.
- [dotnet/runtime #121536](https://github.com/dotnet/runtime/issues/121536) — Tracking issue for adding async APIs to `System.ComponentModel.DataAnnotations`. The Blazor infrastructure designed in this spec is intended to support these BCL async APIs when they land.
- [dotnet/designs #363](https://github.com/dotnet/designs/pull/363) — Design draft for adding async validation support in the BCL. Defines the `AsyncValidationAttribute` and `Validator.TryValidateObjectAsync` APIs that Blazor will consume.
- [dotnet/aspnetcore #64609](https://github.com/dotnet/aspnetcore/issues/64609) — Tracking issue for async validation support in Minimal APIs. The Minimal API and Blazor async validation efforts share the `Microsoft.Extensions.Validation` infrastructure.
- [dotnet/aspnetcore #51501](https://github.com/dotnet/aspnetcore/issues/51501) — Community proposal for adding `ValidateAsync` to `EditContext`. Shows a concrete API sketch that influenced the `OnValidationRequestedAsync` event design.
- [dotnet/aspnetcore #40244](https://github.com/dotnet/aspnetcore/issues/40244#issuecomment-1044298329) — Workaround sketch for doing async form validation with current Blazor API. Illustrates the amount of boilerplate developers must write today and the UX tradeoffs involved.
- [Blazored #38](https://github.com/Blazored/FluentValidation/issues/38) — Discussion about issues and workarounds while using async FluentValidation validators with current Blazor API. Documents the core pain point: forms submit before async validation completes.
- [Blazored #31](https://github.com/Blazored/FluentValidation/issues/31) — Earlier discussion about async FluentValidation validator issues with Blazor, including community-proposed workarounds and the limitations of the sync event model.
- [Blazilla](https://github.com/loresoft/Blazilla/) — Form validator package that works around the missing async features in the current Blazor API. Demonstrates one approach (replacing `EditForm`) and validates that the problem is solvable at the framework level.
- [Angular Async Validators](https://angular.dev/guide/forms/form-validation) — Angular's async validation with built-in `PENDING` state and `ng-pending` CSS class. Serves as prior art for the pending state, CSS class, and cancellation patterns proposed here.
- [Angular Signal Forms — Async Operations](https://angular.dev/guide/forms/signals/async-operations) — Angular's latest async validation guide covering the `pending()` API and async validator lifecycle. Serves as prior art for the programmatic pending state query pattern.
- [react-hook-form — useFormState](https://react-hook-form.com/docs/useformstate/) — React Hook Form's `isValidating` state for tracking in-progress async validation. Serves as prior art for the programmatic pending state query pattern.
