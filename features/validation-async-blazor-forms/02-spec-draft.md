# Async Blazor Form Validation — Specification Draft

> **Purpose**: This document lists goals and scenarios for async validation support in Blazor forms. It focuses on *what* developers need to accomplish, not *how* it should be implemented. We will trim and prioritize these items before moving to design.

## Table of Contents

1. [Async Form-Submit Validation](#1-async-form-submit-validation)
2. [Async Per-Field Validation](#2-async-per-field-validation)
3. [Pending Validation State](#3-pending-validation-state)
4. [CSS Classes for Validation State](#4-css-classes-for-validation-state)
5. [Cancellation of Stale Validations](#5-cancellation-of-stale-validations)
6. [Third-Party Validator Integration](#6-third-party-validator-integration)
7. [Integration with Microsoft.Extensions.Validation](#7-integration-with-microsoftextensionsvalidation)
8. [Backward Compatibility](#8-backward-compatibility)
9. [Blazor WebAssembly Support](#9-blazor-webassembly-support)
10. [Static SSR Forms](#10-static-ssr-forms)
11. [Validation Task Tracking](#11-validation-task-tracking)
12. [Incremental Error Reporting](#12-incremental-error-reporting)
13. [Submit Button / Form Interaction During Validation](#13-submit-button--form-interaction-during-validation)
14. [Custom Async Validator Components](#14-custom-async-validator-components)
15. [Sync-First, Then Async Optimization](#15-sync-first-then-async-optimization)
16. [Debouncing](#16-debouncing)
17. [Security Considerations](#17-security-considerations)

---

## 1. Async Form-Submit Validation

### Scenario

A developer has a form with fields that require server-side validation (e.g., username uniqueness check via an HTTP call). When the user clicks "Submit", the form should:
1. Run all validation (sync and async)
2. Wait for all async validation to complete
3. Only then invoke `OnValidSubmit` or `OnInvalidSubmit`

Today, `EditForm` calls `EditContext.Validate()` synchronously, which means async validators either don't run at all, or run *after* the form has already been submitted — leading to invalid data being accepted.

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
- [Blazored/FluentValidation#38](https://github.com/Blazored/FluentValidation/issues/38) — Forms submit before async validation completes

---

## 2. Async Per-Field Validation

### Scenario

A developer wants to validate individual fields asynchronously as the user interacts with them (on change, on blur, or with a debounce). For example:
- User types an email address → after they stop typing (or blur), check via API if the email is already registered
- Show validation errors inline next to the field as they arrive

### User needs

- Async validation should trigger on field change (or blur), not just on form submit
- Results should appear inline via `<ValidationMessage>` just like sync validation results
- Multiple fields can validate asynchronously and concurrently
- If a field is edited again while async validation is running, the previous result should be discarded

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

With a custom async validation attribute (once BCL support lands):
```csharp
public class UniqueEmailAttribute : AsyncValidationAttribute
{
    protected override async ValueTask<ValidationResult?> IsValidAsync(
        object? value, ValidationContext ctx, CancellationToken ct)
    {
        var email = value as string;
        if (string.IsNullOrEmpty(email)) return ValidationResult.Success;

        var userService = ctx.GetRequiredService<IUserService>();
        var exists = await userService.EmailExistsAsync(email, ct);

        return exists
            ? new ValidationResult("This email is already registered.")
            : ValidationResult.Success;
    }
}

public class Registration
{
    [Required]
    [UniqueEmail]
    public string Email { get; set; } = "";
}
```

### References
- [#40244 comment by @SteveSandersonMS](https://github.com/dotnet/aspnetcore/issues/40244#issuecomment-1044298329) — Manual per-field async workaround
- Angular: async validators run on field change automatically

---

## 3. Pending Validation State

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

### References
- Angular: `control.pending` property, `FormGroup.pending`
- react-hook-form: `formState.isValidating`
- [#7680](https://github.com/dotnet/aspnetcore/issues/7680) — `HasPendingValidationTasks()` in original design

---

## 4. CSS Classes for Validation State

### Scenario

Blazor's `InputBase<T>` applies CSS classes based on validation state (`valid`, `invalid`, `modified`). When async validation is pending, there should be a corresponding CSS class so developers can style the field accordingly (e.g., orange border, spinner background image) using pure CSS.

### User needs

- A `"pending"` CSS class (or equivalent) should be automatically applied to input fields while async validation runs
- The class should be removed when validation completes (and replaced with `valid` or `invalid`)
- Must work with `FieldCssClassProvider` customization (e.g., for Bootstrap integration: `is-validating`, or any custom name)
- Both field-level and form-level CSS states should be derivable

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

## 5. Cancellation of Stale Validations

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

## 6. Third-Party Validator Integration

### Scenario

Developers using third-party validation libraries (especially FluentValidation) need async validation to work seamlessly with Blazor forms. The most common case: FluentValidation's `MustAsync()` and `WhenAsync()` rules.

### User needs

- FluentValidation async validators should work correctly with `EditForm` — forms must not submit before async validators complete
- Library authors should have an async-capable hook to subscribe to validation requests
- The existing pattern of creating validator components (like `<DataAnnotationsValidator />`) should extend naturally to async scenarios
- Libraries should not need to fork `EditForm` or use `async void` event handlers

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

## 7. Integration with Microsoft.Extensions.Validation

### Scenario

The new `Microsoft.Extensions.Validation` package (`src/Validation/`) provides an async-first validation infrastructure with `IValidatableInfo.ValidateAsync()`. Blazor's `EditContextDataAnnotationsExtensions` already calls this but currently **throws if it doesn't complete synchronously**. This guard should be removed so the Validation package's async capabilities flow through to Blazor forms.

### User needs

- `DataAnnotationsValidator` should use `Microsoft.Extensions.Validation`'s async pipeline without blocking or throwing
- Validation errors discovered during async traversal should stream into `ValidationMessageStore` in real-time via the `OnValidationError` event
- Complex object graph validation (nested objects, collections) should work asynchronously
- Source-generated validators should participate in async validation

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

## 8. Backward Compatibility

### Scenario

Millions of existing Blazor applications use the current synchronous validation pipeline. Introducing async validation must not break any existing functionality.

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

### Non-goals (explicitly)

- Changing the existing `Validate()` return type or signature
- Removing synchronous validation support
- Requiring developers to use `ValidateAsync()` for sync-only scenarios

---

## 9. Blazor WebAssembly Support

### Scenario

On Blazor WebAssembly, `HttpClient` is async-only — there is no synchronous HTTP API. This means any validation that needs to call a server endpoint (username check, business rule validation) **must** be async. Sync-over-async workarounds (`Task.Run().GetAwaiter().GetResult()`) deadlock on WASM's single-threaded runtime.

### User needs

- Async validation must work correctly on Blazor WASM (single-threaded, async-only HTTP)
- No sync-over-async blocking anywhere in the validation pipeline
- Validation that calls the server via `HttpClient` must work naturally

### Desired developer experience

```razor
@inject HttpClient Http

<EditForm Model="@profile" OnValidSubmit="SaveProfile">
    <DataAnnotationsValidator />

    <InputText @bind-Value="profile.DisplayName" />
    <ValidationMessage For="() => profile.DisplayName" />

    <button type="submit">Save</button>
</EditForm>

@code {
    private UserProfile profile = new();

    // This custom async validator calls the server — must work on WASM
    // without any sync-over-async hacks
}
```

### References
- [Blazored/FluentValidation#38](https://github.com/Blazored/FluentValidation/issues/38) — WASM-specific deadlock reports
- Research §9.5 — Blazor WASM constraints

---

## 10. Static SSR Forms

### Scenario

Blazor .NET 8+ supports Static Server Rendering (SSR) with enhanced forms. These forms use HTTP POST and don't have a persistent WebSocket connection. Async validation must work in this context too.

### User needs

- When a form is submitted via SSR enhanced navigation, async validation should run server-side before the response is sent
- No interactive UI updates are possible during SSR form processing (no spinners) — validation must complete fully before rendering the response
- The response should include validation errors if async validation fails

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

## 11. Validation Task Tracking

### Scenario

Multiple async validations can be in-flight simultaneously (different fields, or both per-field and per-form validation). The system needs to track these tasks so it can:
- Know when all validations have completed
- Report pending state per-field and per-form
- Cancel specific field validations when the field value changes

### User needs

- The framework should track all in-flight async validation tasks
- Per-field tracking: which fields currently have pending validations?
- Per-form tracking: are there any pending validations anywhere in the form?
- Completion signal: await all pending validations (used by `ValidateAsync()` and form submission)
- Validators (built-in or custom) should be able to register their async tasks with the tracking system

### Desired developer experience

```csharp
// Inside a custom validator component
protected override void OnInitialized()
{
    EditContext.OnFieldChanged += async (sender, args) =>
    {
        using var task = EditContext.AddValidationTask(args.FieldIdentifier);
        // Perform async validation...
        // The task is tracked until this scope completes
    };
}

// Inside EditForm or custom form submission logic
var isValid = await editContext.ValidateAsync();
// ValidateAsync() awaits all pending validation tasks before returning
```

```csharp
// Querying validation state
bool anyPending = editContext.IsValidationPending();
bool fieldPending = editContext.IsValidationPending(fieldIdentifier);
```

### References
- [#7680](https://github.com/dotnet/aspnetcore/issues/7680) — `AddValidationTask`, `HasPendingValidationTasks`, `WhenAllValidationTasks`

---

## 12. Incremental Error Reporting

### Scenario

During async validation of a complex object graph (e.g., an order with many items), errors may be discovered progressively. The UI should update as each error is found, rather than waiting for all validation to complete before showing anything.

### User needs

- As async validation discovers errors, they should appear in the UI immediately (not batched until the end)
- `<ValidationMessage>` and `<ValidationSummary>` should update in real-time as errors stream in
- This should work automatically via the existing `NotifyValidationStateChanged` mechanism

### Desired developer experience

```razor
<!-- Errors appear one by one as they are discovered during async validation -->
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

## 13. Submit Button / Form Interaction During Validation

### Scenario

While async validation is running (whether triggered by field change or form submission), the developer may want to:
- Disable the submit button
- Show a loading state on the submit button
- Prevent duplicate form submissions
- Show a form-level "Validating…" indicator

### User needs

- Easy way to disable the submit button while validation is pending
- Easy way to show loading state during validation + submission
- The form should not submit twice if the user clicks rapidly
- The framework should provide enough state for the developer to implement these patterns without manual boolean flags

### Desired developer experience

```razor
<EditForm Model="@user" OnValidSubmit="HandleSubmit" Context="formContext">
    <DataAnnotationsValidator />

    <InputText @bind-Value="user.Username" />
    <ValidationMessage For="() => user.Username" />

    <!-- Submit button automatically reflects validation state -->
    <button type="submit"
            disabled="@formContext.IsValidationPending()">
        @if (formContext.IsValidationPending())
        {
            <span class="spinner-border spinner-border-sm"></span>
            <text>Validating…</text>
        }
        else
        {
            <text>Register</text>
        }
    </button>
</EditForm>
```

Or with a higher-level approach via `EditForm` exposing the state:
```razor
<EditForm Model="@user" OnValidSubmit="HandleSubmit">
    <DataAnnotationsValidator />
    <!-- ... fields ... -->

    <SubmitButton>Register</SubmitButton>
    <!-- SubmitButton could be a built-in component that auto-disables during validation -->
</EditForm>
```

### References
- [Preventing double form submission](https://www.meziantou.net/preventing-double-form-submission-in-a-blazor-application.htm)
- Angular: submit button can be bound to `form.pending`
- Research §7.7 — Current manual workaround with `isSubmitting` flag

---

## 14. Custom Async Validator Components

### Scenario

Developers should be able to create custom validator components (like `<DataAnnotationsValidator />` but with custom logic) that participate in async validation. This is the primary extensibility model for Blazor form validation.

### User needs

- Validator components should be able to register async validation handlers
- The async handler should be properly awaited by `ValidateAsync()` before the form determines validity
- Multiple validator components can coexist on the same form (e.g., `<DataAnnotationsValidator />` + `<CustomRemoteValidator />`)
- Validator components should follow the same patterns as existing sync validator components

### Desired developer experience

```csharp
public class RemoteValidator : ComponentBase, IDisposable
{
    [CascadingParameter] EditContext EditContext { get; set; }
    [Parameter] public string ValidationEndpoint { get; set; }

    private ValidationMessageStore messageStore;

    protected override void OnInitialized()
    {
        messageStore = new ValidationMessageStore(EditContext);

        // Register an async validation handler that EditContext will await
        EditContext.OnValidationRequestedAsync += ValidateAsync;
    }

    private async Task ValidateAsync(object sender, ValidationRequestedEventArgs args)
    {
        messageStore.Clear();

        var response = await Http.PostAsJsonAsync(ValidationEndpoint, EditContext.Model);
        if (!response.IsSuccessStatusCode)
        {
            var problems = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
            foreach (var (field, errors) in problems.Errors)
            {
                var fieldId = EditContext.Field(field);
                foreach (var error in errors)
                    messageStore.Add(fieldId, error);
            }
        }

        EditContext.NotifyValidationStateChanged();
    }

    public void Dispose()
    {
        EditContext.OnValidationRequestedAsync -= ValidateAsync;
    }
}
```

```razor
<EditForm Model="@order" OnValidSubmit="Submit">
    <DataAnnotationsValidator />
    <RemoteValidator ValidationEndpoint="/api/validate/order" />

    <!-- Both validators run — form only submits when both pass -->
</EditForm>
```

### References
- Existing pattern: `DataAnnotationsValidator` subscribes to `OnValidationRequested`
- [#51501](https://github.com/dotnet/aspnetcore/issues/51501) — Proposed async event handler pattern

---

## 15. Sync-First, Then Async Optimization

### Scenario

Async validation is typically expensive (network calls, database queries). It should not run if synchronous validation has already found errors — there's no point checking username uniqueness if the field is empty.

### User needs

- Synchronous validation should run first
- Async validation should only run if synchronous validation passes (for the same field or form)
- This is an optimization, not a hard requirement — but it follows Angular's proven pattern and avoids unnecessary server calls

### Desired developer experience

```csharp
public class Registration
{
    [Required]                    // Sync — runs first
    [MinLength(3)]                // Sync — runs first
    [UniqueUsername]               // Async — only runs if Required and MinLength pass
    public string Username { get; set; } = "";

    [Required]                    // Sync — runs first
    [EmailAddress]                // Sync — runs first
    [UniqueEmail]                 // Async — only runs if Required and EmailAddress pass
    public string Email { get; set; } = "";
}
```

The framework should handle this ordering automatically — the developer just declares both sync and async attributes, and the framework optimizes execution order.

### References
- Angular: *"Async validators only run after all synchronous validators pass"*
- Research §6.1 — Angular's optimization strategy

---

## 16. Debouncing

### Scenario

For per-field async validation triggered on every keystroke, making an HTTP call for each character would overwhelm the server and create a poor UX. Debouncing delays the validation until the user stops typing for a configurable period.

### User needs

- Option to debounce async field validation (e.g., wait 300ms after last keystroke before firing validation)
- This should be configurable per field or globally
- During the debounce delay, no validation should run and no pending state should be shown (or a separate "waiting" state could be shown — debatable)
- This is a nice-to-have at the framework level — libraries and developers can implement it themselves if needed

### Desired developer experience (if framework-supported)

```csharp
// Option A: Attribute-based
public class Registration
{
    [Required]
    [UniqueUsername(DebounceMs = 300)]
    public string Username { get; set; } = "";
}

// Option B: Component parameter
<InputText @bind-Value="registration.Username" ValidationDebounceMs="300" />
```

### Note

This may be better left to libraries and developer code rather than built into the framework. Angular supports it via `updateOn: 'blur'`, react-hook-form requires manual debounce. The question is whether Blazor should provide this as a first-class feature or leave it to the ecosystem.

### References
- Angular: `updateOn: 'blur'` option
- react-hook-form: Manual debounce with `lodash.debounce`
- Research §6.2 — React debounce + cancel pattern

---

## 17. Security Considerations

### Scenario

Async validation that checks for resource existence (e.g., "is this email registered?") can leak information. An attacker could use the validation endpoint to enumerate valid emails or usernames.

### User needs

- Developers should be aware of enumeration risks when implementing uniqueness checks
- Framework documentation should call out this concern
- Consider recommending rate limiting, generic error messages, or CAPTCHA for sensitive validation

### Note

This is not a framework implementation concern per se, but should be addressed in documentation and sample code.

### References
- [Security note from community blog](https://jonhilton.net/blazor-client-server-validation-with-fluentvalidation/)

---

## Summary — Full Wishlist

| # | Goal | Priority Guess | Notes |
|---|---|---|---|
| 1 | Async form-submit validation (`ValidateAsync`) | **Must have** | Core feature — forms don't submit until async validates |
| 2 | Async per-field validation | **Must have** | Triggered on field change/blur |
| 3 | Pending validation state (per-field + per-form) | **Must have** | Needed for any meaningful UX |
| 4 | CSS classes for pending state | **Should have** | Enables pure-CSS feedback, follows Angular precedent |
| 5 | Cancellation of stale validations | **Must have** | Prevents wrong results from appearing |
| 6 | Third-party validator integration | **Must have** | FluentValidation is the top user demand |
| 7 | Integration with Microsoft.Extensions.Validation | **Must have** | Remove the async throw guard |
| 8 | Backward compatibility | **Must have** | Cannot break existing apps |
| 9 | Blazor WASM support | **Must have** | Async-only HTTP makes this a hard requirement |
| 10 | Static SSR forms | **Should have** | Async validation runs server-side during POST |
| 11 | Validation task tracking | **Must have** | Infrastructure for pending state and awaiting |
| 12 | Incremental error reporting | **Nice to have** | Errors stream in as discovered |
| 13 | Submit button / form interaction | **Should have** | Disable during validation |
| 14 | Custom async validator components | **Must have** | Primary extensibility model |
| 15 | Sync-first, then async optimization | **Should have** | Avoids wasted async calls |
| 16 | Debouncing | **Nice to have** | May be better left to ecosystem |
| 17 | Security considerations | **Documentation** | Not a code change |
