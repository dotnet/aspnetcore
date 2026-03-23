# Async Validation Support for Blazor Forms

## Summary

This document proposes adding async validation support to Blazor's form validation infrastructure (`EditContext`, `EditForm`, `DataAnnotationsValidator`, and related components) to address the inability to perform I/O-bound validation operations — such as database uniqueness checks, remote API calls, or service-based business rule validation — within the standard Blazor form validation pipeline.

Today, the entire Blazor form validation pipeline is synchronous. `EditContext.Validate()` fires a synchronous event and returns `bool`. Developers who need async validation are forced to block on async operations using `.Result` or `.GetAwaiter().GetResult()`, which is incompatible with Blazor WebAssembly and causes deadlocks on Blazor Server. Third-party libraries like FluentValidation — which has first-class async support — cannot fully integrate with Blazor's `EditContext` because the event model is synchronous. Additionally, async validation APIs are being added to `System.ComponentModel.DataAnnotations` in the BCL ([dotnet/runtime#121536](https://github.com/dotnet/runtime/issues/121536)); the Blazor form infrastructure needs to be ready to support these when they land.

This proposal adds `EditContext.ValidateAsync()`, an async event model for validation requests, per-field async validation task tracking with pending state and cancellation, and UI feedback mechanisms including CSS classes and an optional indicator component.

## Goals

- **Enable async validation on form submit.** Developers need forms that await async validators (e.g., server-side uniqueness checks) before invoking `OnValidSubmit` or `OnInvalidSubmit`, for both simple models and complex object graphs with nested objects and collections. Today, forms either submit before async validation completes or require sync-over-async blocking that deadlocks.

- **Enable async validation on field change.** Developers need individual fields to validate asynchronously as the user interacts with them (on change or blur), with errors appearing inline via `<ValidationMessage>` — the same way sync validation works today.

- **Expose pending and faulted validation state.** While async validation is in progress, developers need to query pending state per-field and per-form to show spinners, "Checking…" text, or disable submit buttons. When async validation fails due to infrastructure errors (network failure, timeout, unhandled exception in the validator), the field should enter a "faulted" state distinct from both "valid" and "invalid" — enabling UI like "An error occurred while validating, please try again." Both states must trigger UI re-renders automatically.

- **Cancel stale validations.** When a user edits a field while its async validation is in progress, the previous validation must be cancelled (or its result discarded) so that only the validation result for the current field value is ever displayed.

- **Maintain full backward compatibility.** Existing applications using sync-only validators must continue to work without any runtime regressions or noticeable performance overhead.

- **Improve third-party async validator integration.** Library authors or integrators (e.g., FluentValidation) need an async-capable extension point to hook into the validation pipeline without forking `EditForm` or resorting to `async void` event handlers.

- **Support all Blazor rendering modes.** Async validation must work across interactive Server, WebAssembly, and Auto rendering modes — where `EditForm.HandleSubmitAsync()` is already async and the feature works naturally — as well as Static SSR, where there is no persistent connection and validation must complete fully before the response is rendered.

## Non-goals

- **BCL-level async validation APIs.** Adding async validation APIs to `System.ComponentModel.DataAnnotations` is tracked separately ([dotnet/runtime#121536](https://github.com/dotnet/runtime/issues/121536), [dotnet/designs#363](https://github.com/dotnet/designs/pull/363)). This spec focuses on the Blazor-side changes that can proceed independently.

- **Parallel execution strategy for validators.** Whether async validators on the same property run sequentially or concurrently, and how error short-circuiting works, is outside the scope of this specification. The execution strategy is determined by the validation infrastructure (`Microsoft.Extensions.Validation`, `System.ComponentModel.DataAnnotations`) and is expected to produce acceptable UX — particularly for collection validation where sequential execution would cause cumulative latency.

- **Incremental error reporting during form-submit validation.** The built-in `DataAnnotationsValidator` will not stream errors progressively as each field's async validation completes during form submission. Instead, all validation results are batched and reported together once the entire validation pass finishes. Streaming errors one-by-one causes layout shifts and unpredictable UX as the error list grows during validation. Third-party validator components are free to implement incremental reporting via the `ValidationMessageStore` and `NotifyValidationStateChanged()` APIs if they choose to.

- **Built-in debouncing for async field validation.** Blazor's built-in input components bind to `onchange` (which fires on blur), not `oninput` (every keystroke). Since async validation already triggers on blur by default, debouncing is unnecessary for the common case. Using async validators with `oninput` binding is not recommended. Developers who need debounced validation (e.g., with `oninput`) can implement the behavior by e.g. delaying the calls to `NotifyFieldChanged`. Built-in debouncing will be reconsidered as part of a future general event debounce feature for Blazor.

- **Remote client-side validation.** This proposal does not introduce a mechanism for running async validators on the client that call back to the server. Async validation runs where the Blazor component executes — on the server for Server and SSR modes, or in the browser for WebAssembly. There is no new client-to-server validation channel.

## Proposed solution

The solution adds async validation capabilities to Blazor forms through two complementary mechanisms on `EditContext`:

1. **`OnValidationRequestedAsync`** — a new async event that is the counterpart to the existing sync `OnValidationRequested`. Validator components subscribe to this event to run async validation on form submit. `EditContext.ValidateAsync()` invokes and awaits all handlers.

2. **`AddValidationTask(FieldIdentifier, Task)`** — a method for tracking in-flight async validation tasks per field. This enables pending state queries (`IsValidationPending`), automatic cancellation when a field is re-edited, and automatic cancellation on form submit (where full-form validation supersedes field-level tasks).

`EditForm.HandleSubmitAsync()` is updated to call `ValidateAsync()` instead of `Validate()`. UI feedback is provided through `"pending"` and `"faulted"` CSS classes on `InputBase` (via `FieldCssClassProvider`) and optional render fragments on `ValidationMessage`.

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

### Scenario 1: Form submission with async validation

This scenario covers the core submit-path flow: implicit validation via `OnValidSubmit`, explicit validation via `OnSubmit` + `ValidateAsync()`, and backward compatibility for existing sync-only forms.

**Implicit submit validation.** A form with async validators awaits all validation before invoking `OnValidSubmit`. No changes to the developer's form code are needed — `EditForm` internally calls `ValidateAsync()` instead of `Validate()`.

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

**Explicit submit validation.** A developer using `OnSubmit` can await async validation explicitly via `EditContext.ValidateAsync()`. This invokes both sync `OnValidationRequested` handlers and async `OnValidationRequestedAsync` handlers, then returns `!GetValidationMessages().Any()`.

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

**Backward compatibility.** Existing forms with sync-only validators continue to work identically at runtime. `EditForm` internally uses `ValidateAsync()`, which invokes both existing sync `OnValidationRequested` handlers and the new async `OnValidationRequestedAsync` handlers — so existing validator components continue to work without changes.

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

**Behavior of sync `Validate()`.** There are two alternative designs under consideration:

**Alternative A — Mark `Validate()` as `[Obsolete]`:**

```csharp
var isValid = editContext.Validate();
// Warning CS0618: 'EditContext.Validate()' is obsolete. Use 'ValidateAsync()' instead.
```

**Alternative B — Add an optional `syncOnly` parameter:**

```csharp
var isValid = editContext.Validate();              // throws if model has async validators
var isValid = editContext.Validate(syncOnly: true); // skips async validators, no throw
var isValid = await editContext.ValidateAsync();    // full validation including async
```

In both alternatives, if the model has async validators and the developer calls `Validate()` without opting into sync-only mode, the validation infrastructure throws `InvalidOperationException` at runtime. This prevents silent data loss where async validators would be silently skipped.

> **Open question:** Should the existing sync `Validate()` be marked `[Obsolete]` (Alternative A) or gain an optional `syncOnly` parameter (Alternative B)? Alternative B preserves it as a non-obsolete API for intentional sync-only validation. Alternative A provides a cleaner migration signal.

### Scenario 2: Field-level validation and validation state UX

This scenario covers async validation triggered by field changes, and the ways developers can observe and display validation state — declaratively, programmatically, and via CSS classes.

**Field-triggered async validation.** A field with an async validator shows inline errors as the user interacts with it, with no additional code beyond declaring the validator attributes on the model.

```razor
<EditForm Model="@registration">
    <DataAnnotationsValidator />

    <InputText @bind-Value="registration.Email" />
    <ValidationMessage For="() => registration.Email" />
    ...
</EditForm>
```

No manual wiring is needed — `DataAnnotationsValidator` handles the async validation lifecycle automatically.

**Declarative state display.** The proposed approach extends `ValidationMessage` with optional `PendingContent` and `FaultedContent` render fragments. When async validation is pending, `PendingContent` is rendered instead of error messages. When validation has faulted (network error, timeout), `FaultedContent` is rendered. When the render fragments are not provided, `ValidationMessage` behaves exactly as it does today.

```razor
<InputText @bind-Value="registration.Username" />
<ValidationMessage For="() => registration.Username">
    <PendingContent>
        <span class="spinner">Checking availability…</span>
    </PendingContent>
    <FaultedContent>
        <span class="text-warning">An error occurred while validating. Please try again.</span>
    </FaultedContent>
</ValidationMessage>
```

> **Open question:** `ValidationMessage` currently does not accept render fragments — it renders a flat list of `<div class="validation-message">` elements. Adding `PendingContent` and `FaultedContent` could be a breaking change in the rendered DOM. Alternatives: (1) a separate `ValidationPendingIndicator` component, (2) leave to developers via programmatic APIs.

**Programmatic state query.**Developers can query pending and faulted state directly on `EditContext` for custom UI such as disabling the submit button or showing form-level indicators.

```razor
<div class="form-group">
    <InputText @bind-Value="registration.Username" />

    @if (editContext.IsValidationPending(editContext.Field(nameof(registration.Username))))
    {
        <span class="spinner">Checking availability…</span>
    }
    else if (editContext.IsValidationFaulted(editContext.Field(nameof(registration.Username))))
    {
        <span class="text-warning">An error occurred while validating. Please try again.</span>
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
```

`IsValidationPending(FieldIdentifier)` returns `true` while a tracked async task is running for that field. `IsValidationPending()` (no args) returns `true` if any field has a pending task. `IsValidationFaulted(FieldIdentifier)` returns `true` when a validation task failed due to an infrastructure error (see Scenario 4 for the detailed distinction between cancellation and faults).

**CSS classes.** `InputBase` automatically applies `"pending"` or `"faulted"` CSS classes via `FieldCssClassProvider`, following the existing compound class pattern (`"modified pending"`, `"modified faulted"`).

```css
/* New CSS classes for async validation state */
input.pending {
    border-color: orange;
    background-image: url('/spinner.svg');
}

input.faulted {
    border-color: #cc7700;
}

/* Existing classes (valid, invalid, modified) continue to work as before */
```

Custom `FieldCssClassProvider` subclasses can map these states to any CSS class:

```csharp
public class BootstrapFieldCssClassProvider : FieldCssClassProvider
{
    public override string GetFieldCssClass(EditContext editContext, in FieldIdentifier fieldIdentifier)
    {
        if (editContext.IsValidationPending(fieldIdentifier))
            return "is-validating";

        if (editContext.IsValidationFaulted(fieldIdentifier))
            return "is-warning";

        var isValid = !editContext.GetValidationMessages(fieldIdentifier).Any();
        if (!editContext.IsModified(fieldIdentifier))
            return "";

        return isValid ? "is-valid" : "is-invalid";
    }
}
```

### Scenario 3: Complex object graphs and execution model

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

[ValidatableType]
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
    @* ... fields for order and order items ... *@
    <ValidationSummary />
    <button type="submit">Place Order</button>
</EditForm>
```

Both the legacy `System.ComponentModel.Validator` path and the `Microsoft.Extensions.Validation` path are retained for now. If `AddValidation()` is not called, the validation falls back to the static `Validator` path, which only validates the root model instance and its properties.

### Scenario 4: Cancellation, faults, submission races, and retry

This scenario covers all cases where async validation work becomes obsolete, fails to complete normally, or needs to be re-run.

**User-initiated cancellation.** When the user edits a field while its async validation is still running, the previous task is cancelled via `CancellationToken` (`OperationCanceledException`). The result is silently discarded and a new validation starts. The field does **not** enter "faulted" state — it transitions back to "pending" with the new validation. This is cancellation in the `CancellationToken` sense: the work was intentionally abandoned because it is no longer relevant.

**Validation faults.** When the async validator throws a non-cancellation exception (network error, timeout, unhandled error), the field enters "faulted" state, queryable via `IsValidationFaulted()` and reflected in CSS classes. A faulted validation means the infrastructure failed to determine validity — it does not mean the field's value is invalid. The faulted state is cleared when the field is edited again.

These two cases are distinct and should not be conflated. Cancellation is expected and silent; a fault is an infrastructure error that the user may need to act on (e.g., retry).

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

**Submission while field validation is pending.** When the user submits the form while per-field async validations are still in progress, the pending field-level validations are cancelled because the form-submit validation supersedes them. `ValidateAsync()` re-validates everything from scratch — there is no need to wait for the field-level tasks to finish first.

```
User blurs Username field → async field validation starts (pending)
User immediately clicks Submit:
  1. Pending Username validation is cancelled
  2. Full-form ValidateAsync() runs — re-validates all fields from scratch
  3. OnValidSubmit or OnInvalidSubmit fires based on the full-form result
```

**Retry after fault.** When async validation for a field faults, the developer or user needs a way to retry without changing the field value and without re-validating the entire form.

```razor
<div class="form-group">
    <InputText @bind-Value="registration.Username" />

    @if (editContext.IsValidationFaulted(editContext.Field(nameof(registration.Username))))
    {
        <span class="text-warning">
            An error occurred while validating.
            <button @onclick="() => editContext.ValidateFieldAsync(editContext.Field(nameof(registration.Username)))">
                Retry
            </button>
        </span>
    }
</div>
```

`ValidateFieldAsync(FieldIdentifier)` is a new method proposed on `EditContext`. It triggers validation for a specific field — clears any faulted state, runs validators, and registers the async task for tracking. The field transitions from "faulted" → "pending" → "valid" or "invalid".

> **Open question:** When an `OnValidationRequestedAsync` handler throws during form-submit validation, what happens? Options: (1) the exception propagates out of `EditForm.HandleSubmitAsync`, (2) the form enters a form-level "faulted" state, or (3) the form is treated as invalid.

> **Open question:** Developers could call `NotifyFieldChanged(fieldIdentifier)` to retry validation, but `NotifyFieldChanged` is semantically a value-change notification that marks the field as modified. A dedicated `ValidateFieldAsync` provides correct semantics for retry. Should we add this new API?

### Scenario 5: Extensibility and rendering modes

This scenario covers integration points for third-party validators and behavior across rendering modes, including Static SSR.

**Third-party validator integration.** Third-party validator components subscribe to `OnValidationRequestedAsync` for form-submit validation and use `AddValidationTask` for field-level async validation. The existing pattern of creating validator components extends naturally to async scenarios.

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

From the form developer's perspective, third-party validators work just like `DataAnnotationsValidator`:

```razor
<EditForm Model="@user" OnValidSubmit="HandleSave">
    <FluentValidationValidator />

    <InputText @bind-Value="user.Email" />
    <ValidationMessage For="() => user.Email" />

    <button type="submit">Save</button>
</EditForm>
```

**Static SSR.** In Static Server Rendering mode, async validation runs server-side during form POST processing. Validation must complete fully before the response is rendered — no progressive UI updates are possible. This applies to both enhanced (`Enhance`) and non-enhanced forms — in the non-enhanced case, `ValidateAsync` is awaited during POST processing and the complete result (with any validation errors) is rendered in the full-page response.

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

> **Open question:** For SSR enhanced forms, could the server stream an initial render with the form in "pending" state, then stream a DOM update with the final validation result? This would need to be opt-in and is only applicable with `Enhance`.

## Assumptions

- `Validator.TryValidatePropertyAsync` and `Validator.TryValidateObjectAsync` do not exist yet in the BCL. The async validation path without `AddValidation` depends on this future API ([dotnet/runtime#121536](https://github.com/dotnet/runtime/issues/121536)). Until it ships, a polyfill or alternative implementation will be needed.
- `EditContext` is accessed from a single synchronization context (Blazor's component rendering thread). No concurrent access safeguards are needed.
- The order in which sync and async validators execute for a given field or model is determined by the validation infrastructure (`Microsoft.Extensions.Validation`, `System.ComponentModel.DataAnnotations`), not by Blazor's form components. This spec does not prescribe validator execution order. Note that for the complex object scenario, the execution model can have a significant impact on UX. If async validation over a collection runs sequentially, users pay the cumulative latency of all item validations. The validation infrastructure might need to validate collection items concurrently to mitigate this cost.

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
