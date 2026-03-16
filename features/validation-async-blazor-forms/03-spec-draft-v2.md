# Async Blazor Form Validation — Specification (v2)

> **Purpose**: Goals and scenarios for async validation support in Blazor forms. Focuses on *what* developers need to accomplish, not *how* it should be implemented.

---

## 1. Async Form-Submit Validation

### Scenario

A developer has a form with fields that require server-side validation (e.g., username uniqueness check via a DB query). When the user clicks "Submit", the form should:

1. Run all validation (sync and async)
2. Wait for all async validation to complete
3. Only then invoke `OnValidSubmit` or `OnInvalidSubmit`

Today, `EditForm` calls `EditContext.Validate()` synchronously. Developers who need async validation (e.g., database lookups, HTTP calls) are forced to block on async operations using `.Result` or `.GetAwaiter().GetResult()`. This is incompatible with Blazor WebAssembly and causes known problems for Blazor Server such as deadlocks.

### User needs

- The form must **not submit** until all async validators have completed
- Existing `OnValidSubmit` / `OnInvalidSubmit` callbacks should work naturally with async validation
- Developers who use `OnSubmit` (manual validation control) should be able to await validation themselves

### Desired developer experience

```razor
<!-- This should "just work" — async validation runs before OnValidSubmit fires -->
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

```razor
<!-- For developers using OnSubmit for manual control -->
<EditForm Model="@user" OnSubmit="HandleSubmit">
    <DataAnnotationsValidator />
    <!-- ... -->
</EditForm>

@code {
    private async Task HandleSubmit(EditContext context)
    {
        // Developer can await async validation explicitly
        var isValid = await context.ValidateAsync();
        if (isValid)
        {
            await UserService.CreateUserAsync(user);
        }
    }
}
```

### References
- [#7680](https://github.com/dotnet/aspnetcore/issues/7680) — Original design sketch
- [#51501](https://github.com/dotnet/aspnetcore/issues/51501) — ValidateAsync proposal

---

## 2. Async Per-Field Validation

### Scenario

A developer wants to validate individual fields asynchronously as the user interacts with them (on change, on blur, or with a debounce). For example:
- User types an email address → after they stop typing (or blur), check via API if the email is already registered
- Show validation errors inline next to the field as they arrive

### User needs

- Async validation should trigger on field change (or blur), not just on form submit
- Results should appear inline via `<ValidationMessage>` just like sync validation results

### Open question

When multiple fields trigger async validation concurrently, should they run in parallel or be queued?

### Desired developer experience

```razor
<EditForm Model="@registration">
    <DataAnnotationsValidator />

    <InputText @bind-Value="registration.Email" />
    <ValidationMessage For="() => registration.Email" />

    <InputText @bind-Value="registration.Username" />
    <ValidationMessage For="() => registration.Username" />

    <button type="submit">Register</button>
</EditForm>

@code {
    private Registration registration = new();

    // The async validation is defined on the model via attributes or validators,
    // and runs automatically when the field changes — no manual wiring needed.
}
```

### References
- [#40244 comment by @SteveSandersonMS](https://github.com/dotnet/aspnetcore/issues/40244#issuecomment-1044298329) — Manual per-field async workaround
- [Blazored/FluentValidation#38](https://github.com/Blazored/FluentValidation/issues/38) — Forms submit before async validation completes
- Angular: async validators run on field change automatically

---

## 3. Backward Compatibility

### Scenario

Existing Blazor applications use the current synchronous validation pipeline. Introducing async validation must not break any existing functionality.

### User needs

- `EditContext.Validate()` must continue to work exactly as it does today — synchronous, returns `bool`
- Existing `EventHandler<ValidationRequestedEventArgs> OnValidationRequested` handlers must continue to work
- Existing `DataAnnotationsValidator` usage with sync-only attributes must work unchanged
- Forms using `OnValidSubmit` / `OnInvalidSubmit` / `OnSubmit` must continue to work
- Sync-only validation attributes on models must behave identically
- No new required parameters, no breaking signature changes
- When no async validators are registered, performance should be the same (no `Task` allocation overhead in hot paths)

### Desired developer experience

```razor
<!-- This must continue to work exactly as before — no changes needed -->
<EditForm Model="@person" OnValidSubmit="Save">
    <DataAnnotationsValidator />

    <InputText @bind-Value="person.Name" />
    <ValidationMessage For="() => person.Name" />

    <button type="submit">Save</button>
</EditForm>

@code {
    private Person person = new();

    // Sync validation attributes only — no async overhead
    public class Person
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "";
    }
}
```

---

## 4. Behavior of Sync `Validate()` with Async Validators

### Scenario

A developer has a model with async validation attributes but accidentally calls `EditContext.Validate()` (sync) instead of `ValidateAsync()`. Or, existing code calls `Validate()` on a model that has since gained async attributes.

### User needs

- Clear, predictable behavior — not silent data loss where async validators are silently skipped
- A clear migration path from `Validate()` to `ValidateAsync()`
- Developers should get a clear error message if they call `Validate()` on a model that requires async validation

### Open questions

1. Should this be handled at the Blazor level (in `EditContext.Validate()`) or at the validation infrastructure level (in `Validator.TryValidateObject()` / `ValidatableTypeInfo`)? See the discussion in the BCL async validation proposal (dotnet/designs#363) for context on the runtime-level approach.

2. Should we provide a Roslyn analyzer that detects calls to `EditContext.Validate()` on models with async validation attributes and suggests using `ValidateAsync()` instead?

### Desired developer experience

```csharp
// If model has async validators and you call Validate():
var isValid = editContext.Validate();
// → throws InvalidOperationException:
//   "This model requires async validation. Use ValidateAsync() instead."

// The correct call:
var isValid = await editContext.ValidateAsync();
// → runs both sync and async validators, returns true/false
```

### References
- PRD R13, review discussion between @oroztocil and @danroth27
- MiniValidation: throws `InvalidOperationException` when sync path encounters async-required type
- FluentValidation: throws `AsyncValidatorInvokedSynchronouslyException`

---

## 5. Validation Task Tracking

The system needs a mechanism to coordinate async validation at two levels: form-submit validation (where everything must complete before the form proceeds) and field-level validation (where async work runs in the background).

### Design

Two complementary mechanisms serve different purposes:

**`OnValidationRequestedAsync`** — an async event on `EditContext`, the counterpart to the existing sync `OnValidationRequested`. This is the extensibility point for form-submit validation. `EditContext.ValidateAsync()` invokes and awaits all handlers before determining form validity. Validator components (built-in `DataAnnotationsValidator`, third-party `FluentValidationValidator`, etc.) subscribe to this event.

**`AddValidationTask(FieldIdentifier, Task)`** — a method on `EditContext` that registers an in-flight async validation task for a specific field. This is the coordination mechanism for field-level async validation. It enables pending state queries (Goal 6) and cancellation (Goal 7).

Tracked tasks are **not awaited on submit** — they are cancelled. Full-form validation re-validates everything from scratch, just like the current sync `Validate()` clears all messages and runs `TryValidateObject`. Awaiting tracked tasks before re-validating would cause double work (e.g., waiting for a field-level uniqueness check to finish, then running it again as part of full-model validation).

The existing sync `OnFieldChanged` event remains unchanged. When a field changes, the sync handler runs sync validators immediately, then starts async validators and registers them via `AddValidationTask`. No new `OnFieldChangedAsync` event is needed.

### Flow example

```
Field change (blur/change):
  → EditContext fires sync OnFieldChanged (unchanged)
    → DataAnnotationsValidator handler:
        1. Runs sync validators immediately (Required, MinLength, etc.)
        2. Updates ValidationMessageStore with sync results
        3. Starts async validator (UniqueUsername)
        4. Calls EditContext.AddValidationTask(field, asyncTask)
    → Handler returns
  → UI shows sync errors immediately + pending indicator for async

Async validator completes:
  → Task registered via AddValidationTask finishes
  → Results added to ValidationMessageStore
  → EditContext.NotifyValidationStateChanged() triggers UI update
  → Pending indicator removed, async errors (if any) shown

Field edited again while async running:
  → Tracked task for that field is cancelled (see Goal 7)
  → New sync validation runs, new async task registered

Form submit:
  → EditContext.ValidateAsync():
      1. Cancels all tracked field-level tasks (superseded by full validation)
      2. Clears pending state
      3. Fires OnValidationRequestedAsync, awaits all handlers
         (handlers run full model validation including async validators)
      4. Returns !GetValidationMessages().Any()
```

### References
- [#7680](https://github.com/dotnet/aspnetcore/issues/7680) — `AddValidationTask`, `HasPendingValidationTasks`, `WhenAllValidationTasks`

---

## 6. Pending Validation State

### Scenario

While an async validation operation is in progress, the developer needs to communicate this to the user. The UI might show:
- A spinner next to the field being validated
- "Checking…" text instead of or alongside error messages
- A disabled submit button
- A form-level "Validating…" indicator

### User needs

- Ability to query pending validation state **per field** (is this specific field being validated?)
- Ability to query pending validation state **per form** (is any field in this form being validated?)
- Pending state should trigger UI re-renders automatically (the same way validation state changes trigger re-renders today)
- Components like `InputBase` should be able to reflect pending state in their rendering (e.g., aria attributes, CSS classes)

### Desired developer experience

```razor
<EditForm Model="@registration">
    <DataAnnotationsValidator />

    <div class="form-group">
        <InputText @bind-Value="registration.Username" />

        @if (editContext.IsValidationPending(editContext.Field(nameof(registration.Username))))
        {
            <span class="spinner">Checking availability…</span>
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

Or via a dedicated component:
```razor
<EditForm Model="@registration">
    <DataAnnotationsValidator />

    <InputText @bind-Value="registration.Username" />
    <ValidationPendingIndicator For="() => registration.Username">
        <span class="spinner">Checking…</span>
    </ValidationPendingIndicator>
    <ValidationMessage For="() => registration.Username" />

    <button type="submit">Register</button>
</EditForm>
```

### Open question

Should we provide a built-in component (e.g., `<ValidationPendingIndicator>`) for displaying pending validation state, or is it sufficient to expose the `IsValidationPending()` API and let developers build their own UI?

### References
- Angular: `control.pending` property, `FormGroup.pending`
- react-hook-form: `formState.isValidating`

---

## 7. Cancellation of Stale Validations

### Scenario

A user types into a field that has async validation. The validator starts checking "alice" against the server. Before the response arrives, the user changes the field to "bob". The validation for "alice" is now stale — its result should be discarded, and validation for "bob" should start.

### User needs

- When a field is edited while an async validation for that field is already in progress, the previous validation should be cancelled (or at minimum, its result should be discarded)
- Async validators should receive a `CancellationToken` so they can abort in-flight work (e.g., HTTP requests)
- No stale validation results should appear in the UI after the field value has changed
- Cancellation should not throw — it should silently discard the result

### Desired developer experience

```csharp
public class UniqueUsernameAttribute : AsyncValidationAttribute
{
    protected override async ValueTask<ValidationResult?> IsValidAsync(
        object? value, ValidationContext ctx, CancellationToken cancellationToken)
    {
        var username = value as string;
        if (string.IsNullOrEmpty(username)) return ValidationResult.Success;

        // The CancellationToken is cancelled if the user edits the field again
        // before this call completes — the HTTP request is aborted cleanly
        var userService = ctx.GetRequiredService<IUserService>();
        var exists = await userService.UsernameExistsAsync(username, cancellationToken);

        return exists
            ? new ValidationResult("Username is taken.")
            : ValidationResult.Success;
    }
}
```

From the form's perspective, cancellation is transparent:
```razor
<!-- No special handling needed — the framework cancels stale validations automatically -->
<InputText @bind-Value="user.Username" />
<ValidationMessage For="() => user.Username" />
```

### References
- Angular: Observable unsubscription on value change
- react-hook-form: Counter/AbortController pattern
- `Microsoft.Extensions.Validation`: `CancellationToken` already threaded through `ValidateAsync`

---

## 8. CSS Classes for Validation State

Beyond programmatic queries, developers should be able to style pending fields using pure CSS — no conditional rendering needed for basic visual feedback.

### Scenario

Blazor's `InputBase<T>` applies CSS classes based on validation state (`valid`, `invalid`, `modified`). When async validation is pending, there should be a corresponding CSS class so developers can style the field accordingly (e.g., orange border, spinner background image) using pure CSS.

### User needs

- A `"pending"` CSS class (or equivalent) should be automatically applied to input fields while async validation runs
- The class should be removed when validation completes (and replaced with `valid` or `invalid`)
- Must work with `FieldCssClassProvider` customization (e.g., for Bootstrap integration: `is-validating`, or any custom name)

### Open question

Should we also support a form-level CSS class (e.g., `<form class="validating">`) when any field has pending validation? This would let developers style the entire form differently during validation (e.g., dim the form, show an overlay) using pure CSS.

### Desired developer experience

```css
/* Default Blazor CSS classes — no JavaScript needed */
input.pending {
    border-color: orange;
    background-image: url('/spinner.svg');
    background-repeat: no-repeat;
    background-position: right 8px center;
}

input.valid { border-color: green; }
input.invalid { border-color: red; }
```

With Bootstrap via `FieldCssClassProvider`:
```csharp
public class BootstrapFieldCssClassProvider : FieldCssClassProvider
{
    public override string GetFieldCssClass(EditContext editContext, in FieldIdentifier fieldIdentifier)
    {
        if (editContext.IsValidationPending(fieldIdentifier))
            return "is-validating";

        var isValid = !editContext.GetValidationMessages(fieldIdentifier).Any();
        if (!editContext.IsModified(fieldIdentifier))
            return "";

        return isValid ? "is-valid" : "is-invalid";
    }
}
```

### References
- Angular: `ng-pending`, `ng-valid`, `ng-invalid`, `ng-dirty`, `ng-touched` CSS classes
- Current Blazor: `FieldCssClassProvider.GetFieldCssClass()` returns `valid`/`invalid`/`modified`

---

## 9. Integration with Microsoft.Extensions.Validation

The new `Microsoft.Extensions.Validation` package is already async-first. The question is how Blazor forms should use it — and whether it replaces the legacy `Validator` path.

### Scenario

The new `Microsoft.Extensions.Validation` package (`src/Validation/`) provides an async-first validation infrastructure with `IValidatableInfo.ValidateAsync()`. Blazor's `EditContextDataAnnotationsExtensions` already calls this but currently **throws if it doesn't complete synchronously**. This guard should be removed so the Validation package's async capabilities flow through to Blazor forms.

### User needs

- `DataAnnotationsValidator` should use `Microsoft.Extensions.Validation`'s async pipeline without blocking or throwing
- Complex object graph validation (nested objects, collections) should work asynchronously
- Source-generated validators should participate in async validation

### Open questions

1. Should Blazor forms continue to support the legacy validation path via the static `Validator` class (which works without any DI setup), or should we unify on `Microsoft.Extensions.Validation` as the single validation pipeline? Unifying would simplify the codebase and give us a single async-capable path, but it requires addressing current pain points with `Microsoft.Extensions.Validation` — namely its `[Experimental]` status, the lack of runtime type discovery (only source-generated types are supported today), and the need for explicit `AddValidation()` registration.

2. Should we use the `ValidateContext.OnValidationError` event to stream validation errors into `ValidationMessageStore` in real-time? This event was originally added to enable Blazor's mapping of errors to object instances (used in `FieldIdentifier`s). Is it the right mechanism for real-time error reporting during async validation, or do we need something different?

### Desired developer experience

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

    [UniqueProductInOrder] // Custom async validator checking server-side inventory
    public string ProductId { get; set; } = "";
}
```

```razor
<!-- Deep async validation of Order → Items[i] → ProductId should work automatically -->
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

### References
- `EditContextDataAnnotationsExtensions.cs` line ~166: `if (!validationTask.IsCompleted) throw new InvalidOperationException(...)`
- `src/Validation/src/ValidatableTypeInfo.cs` — Async object graph traversal
- `ValidateContext.OnValidationError` — Event-based error streaming

---

## 10. Third-Party Validator Integration

### Scenario

Developers using third-party validation libraries (especially FluentValidation) need async validation to work seamlessly with Blazor forms. The most common case: FluentValidation's `MustAsync()` and `WhenAsync()` rules.

### User needs

- FluentValidation async validators should work correctly with `EditForm` — forms must not submit before async validators complete
- Library authors should have an async-capable hook to subscribe to validation requests
- The existing pattern of creating validator components (like `<DataAnnotationsValidator />`) should extend naturally to async scenarios
- Libraries should not need to fork `EditForm` or use `async void` event handlers

### Extension point

For form-submit validation, third-party validators subscribe to `OnValidationRequestedAsync` on `EditContext` (see Goal 5). For field-level async validation, they use the existing sync `OnFieldChanged` event and register async work via `AddValidationTask`.

### Desired developer experience

```razor
<EditForm Model="@user" OnValidSubmit="HandleSave">
    <FluentValidationValidator />  <!-- Third-party component -->

    <InputText @bind-Value="user.Email" />
    <ValidationMessage For="() => user.Email" />

    <button type="submit">Save</button>
</EditForm>
```

```csharp
// FluentValidation validator with async rules
public class UserValidator : AbstractValidator<User>
{
    public UserValidator(IUserService userService)
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MustAsync(async (email, ct) => !await userService.EmailExistsAsync(email, ct))
                .WithMessage("Email is already registered.");
    }
}
```

For library authors implementing a validator component:
```csharp
public class FluentValidationValidator : ComponentBase, IDisposable
{
    [CascadingParameter] EditContext EditContext { get; set; }

    protected override void OnInitialized()
    {
        // Library can subscribe to an async validation event/callback
        // that EditContext properly awaits before determining form validity
        EditContext.OnValidationRequestedAsync += async (sender, args) =>
        {
            var validator = GetValidatorForModel(EditContext.Model);
            var result = await validator.ValidateAsync(EditContext.Model);
            // Populate ValidationMessageStore with results
        };
    }
}
```

### References
- [#31905](https://github.com/dotnet/aspnetcore/issues/31905) — FluentValidation author's request (227👍)
- [Blazored/FluentValidation#38](https://github.com/Blazored/FluentValidation/issues/38) — Async validators don't work
- [Blazilla](https://github.com/loresoft/Blazilla/) — Works around the issue by replacing `EditForm`

---

## 11. Static SSR Forms

Async validation must also work in Static Server Rendering mode, where there is no persistent connection for progressive UI updates.

### Scenario

Blazor .NET 8+ supports Static Server Rendering (SSR) with enhanced forms. These forms use HTTP POST and don't have a persistent WebSocket connection. Async validation must work in this context too.

### User needs

- When a form is submitted via SSR enhanced navigation, async validation should run server-side before the response is sent
- No interactive UI updates are possible during SSR form processing (no spinners) — validation must complete fully before rendering the response
- The response should include validation errors if async validation fails

### Open question

Are there any interactions (design-wise) between server-side async validation for SSR forms and the client-side validation feature tracked by [#51040](https://github.com/dotnet/aspnetcore/issues/51040)?

### Desired developer experience

```razor
@* Static SSR form with enhanced navigation *@
<EditForm Model="@contact" FormName="contact" Enhance OnValidSubmit="HandleSubmit">
    <DataAnnotationsValidator />

    <InputText @bind-Value="contact.Email" />
    <ValidationMessage For="() => contact.Email" />

    <button type="submit">Send</button>
</EditForm>

@code {
    [SupplyParameterFromForm]
    private ContactForm contact { get; set; } = new();

    // Async validation (e.g., email uniqueness) runs server-side during form POST processing
    // If validation fails, the page re-renders with validation errors
    private async Task HandleSubmit()
    {
        await ContactService.SaveAsync(contact);
    }
}
```

### References
- [#51040](https://github.com/dotnet/aspnetcore/issues/51040) — Client validation without circuit
- Research §9.7 — Static SSR constraints

---

## 12. Incremental Error Reporting

### Scenario

During form-submit validation of a complex object graph (e.g., an order with many items), async validators on different fields may complete at different times. The question is whether errors should appear in the UI progressively as each field's validation completes, or be batched and shown all at once after the entire form finishes validating.

This goal applies to form-submit validation only. For field-level validation (on blur/change), the pending-to-result transition is covered by Goal 6 (Pending Validation State).

### User needs

- As form-submit async validation discovers errors, they should appear in the UI immediately (not batched until the end)
- `<ValidationMessage>` and `<ValidationSummary>` should update in real-time as errors stream in
- This should work automatically via the existing `NotifyValidationStateChanged` mechanism

### Open questions

1. Should incremental error reporting be the default behavior, or opt-in? Reporting every error as it arrives triggers more re-renders and `NotifyValidationStateChanged` calls.

2. What is the granularity of reporting — each individual error as it is discovered, the complete error set for a property once that property's validation finishes, or the complete error set for the entire model?

3. When a model has a mix of sync and async validators, how are results reported? Do sync errors appear immediately (before async validation starts), or are all results held until everything completes?

### Desired developer experience

```razor
<!-- Errors appear one by one as they are discovered during form-submit async validation -->
<EditForm Model="@order" OnValidSubmit="Submit">
    <DataAnnotationsValidator />

    @for (int i = 0; i < order.Items.Count; i++)
    {
        var item = order.Items[i];
        <InputText @bind-Value="item.ProductId" />
        <ValidationMessage For="() => item.ProductId" />
        <!-- This ValidationMessage updates as soon as item[i]'s async validation completes,
             even if item[i+1] is still validating -->
    }

    <ValidationSummary />
    <button type="submit">Place Order</button>
</EditForm>
```

### References
- `ValidateContext.OnValidationError` — Event-based error streaming in `Microsoft.Extensions.Validation`
- `ValidationMessageStore.Add()` + `EditContext.NotifyValidationStateChanged()` — Existing incremental update mechanism

---

## 13. Debouncing

### Scenario

For per-field async validation triggered on every keystroke, making an HTTP call for each character would overwhelm the server and create a poor UX. Debouncing delays the validation until the user stops typing for a configurable period.

### User needs

- Option to debounce async field validation (e.g., wait 300ms after last keystroke before firing validation)
- This should be configurable per field or globally
- During the debounce delay, no validation should run and no pending state should be shown

### Open question

Should debouncing be built into Blazor's form validation infrastructure, or left for developers and libraries to implement? If left to users, does the framework provide the necessary extensibility points (e.g., field-level control over when async validation triggers) to implement debouncing externally?

### Desired developer experience (if framework-supported)

```razor
<InputText @bind-Value="registration.Username" ValidationDebounce="TimeSpan.FromMilliseconds(300)" />
```

### References
- Angular: `updateOn: 'blur'` option
- react-hook-form: Manual debounce with `lodash.debounce`
- Research §6.2 — React debounce + cancel pattern

---

## Summary

| # | Goal | Priority |
|---|---|---|
| 1 | Async Form-Submit Validation | Must have |
| 2 | Async Per-Field Validation | Must have |
| 3 | Backward Compatibility | Must have |
| 4 | Behavior of Sync `Validate()` with Async Validators | Must have |
| 5 | Validation Task Tracking | Must have |
| 6 | Pending Validation State | Must have |
| 7 | Cancellation of Stale Validations | Must have |
| 8 | CSS Classes for Validation State | Should have |
| 9 | Integration with Microsoft.Extensions.Validation | Must have |
| 10 | Third-Party Validator Integration | Must have |
| 11 | Static SSR Forms | Should have |
| 12 | Incremental Error Reporting | Nice to have |
| 13 | Debouncing | Nice to have |

### Open questions summary

| Goal | Question |
|---|---|
| 2 | When multiple fields trigger async validation concurrently, should they run in parallel or be queued? |
| 4 | Should sync `Validate()` throwing be handled at Blazor or validation infrastructure level? |
| 4 | Should we provide a Roslyn analyzer to detect `Validate()` calls with async models? |
| 6 | Should we provide a built-in `<ValidationPendingIndicator>` component or just the `IsValidationPending()` API? |
| 8 | Should we support a form-level CSS class (e.g., `<form class="validating">`)? |
| 9 | Should we unify on `Microsoft.Extensions.Validation` or keep the legacy `Validator` path? |
| 9 | Should we use `ValidateContext.OnValidationError` for real-time error streaming? |
| 11 | Interactions between server-side async validation and client-side validation (#51040)? |
| 12 | Granularity and default behavior of incremental error reporting? |
| 13 | Should debouncing be built-in or left for users to implement? |
