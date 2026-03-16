# Async Blazor Form Validation — Design Document

> **Based on**: Specification v2 (`03-spec-draft-v2.md`)
> **Scope**: Blazor-specific changes only (EditContext, EditForm, DataAnnotationsValidator, InputBase, CSS)

---

## Decisions Log

| # | Question | Decision |
|---|---|---|
| D1 | Multiple fields async: parallel or queued? | **Parallel** — different fields validate concurrently |
| D2 | Sync `Validate()` with async validators: Blazor or infra level? | **Infrastructure level** — ValidatableTypeInfo throws (existing pattern) |
| D3 | Roslyn analyzer for `Validate()` calls? | **No** — runtime throw is sufficient |
| D4 | Built-in `<ValidationPendingIndicator>` component? | **Yes** — ship a built-in component |
| D5 | Form-level CSS class for pending state? | **No** — removed to avoid CSS class merging complexity on `<form>` |
| D6 | Legacy `Validator` vs `Microsoft.Extensions.Validation`? | **Keep both** for now, plan to unify later |
| D7 | `OnValidationError` for real-time error streaming? | **No** — batch results at the end |
| D8 | SSR / client-side validation interactions? | **No interactions** — independent features |
| D9 | Built-in debounce? | **Yes** — build into the framework |

---

## Overview

This design adds async validation support to Blazor forms through changes to these files:

| File | Changes |
|---|---|
| `EditContext.cs` | Add `ValidateAsync()`, `OnValidationRequestedAsync`, `AddValidationTask()`, `IsValidationPending()`, cancellation tracking |
| `EditForm.cs` | Call `ValidateAsync()` in `HandleSubmitAsync()` |
| `EditContextDataAnnotationsExtensions.cs` | Subscribe to `OnValidationRequestedAsync`, start async field validation via `AddValidationTask`, implement debounce |
| `FieldCssClassProvider.cs` | Add `pending` CSS class support |
| `FieldState.cs` | Track pending validation tasks per field |
| `InputBase.cs` | Add `ValidationDebounce` parameter |
| `ValidationPendingIndicator.cs` | New component |

---

## 1. EditContext Changes

### New public API surface

```csharp
public sealed class EditContext
{
    // === EXISTING (unchanged) ===
    public event EventHandler<FieldChangedEventArgs>? OnFieldChanged;
    public event EventHandler<ValidationRequestedEventArgs>? OnValidationRequested;
    public event EventHandler<ValidationStateChangedEventArgs>? OnValidationStateChanged;
    public bool Validate() { /* unchanged */ }

    // === NEW ===

    /// <summary>
    /// An event that is raised when async validation is requested (e.g., on form submission).
    /// Handlers should perform async validation and populate their ValidationMessageStore.
    /// All handlers are awaited before ValidateAsync() returns.
    /// </summary>
    public event Func<object, ValidationRequestedEventArgs, Task>? OnValidationRequestedAsync;

    /// <summary>
    /// Validates this <see cref="EditContext"/> asynchronously.
    /// Cancels any tracked field-level validation tasks, then invokes both
    /// sync <see cref="OnValidationRequested"/> and async <see cref="OnValidationRequestedAsync"/> handlers.
    /// </summary>
    /// <returns>True if there are no validation messages after validation; otherwise false.</returns>
    public async Task<bool> ValidateAsync()
    {
        // 1. Cancel all tracked field-level tasks — full-form validation supersedes them
        CancelAllValidationTasks();

        // 2. Invoke sync handlers (backward compat — existing validators still work)
        OnValidationRequested?.Invoke(this, ValidationRequestedEventArgs.Empty);

        // 3. Invoke async handlers and await all of them
        if (OnValidationRequestedAsync is not null)
        {
            var delegates = OnValidationRequestedAsync.GetInvocationList();
            var tasks = new Task[delegates.Length];
            for (var i = 0; i < delegates.Length; i++)
            {
                tasks[i] = ((Func<object, ValidationRequestedEventArgs, Task>)delegates[i])
                    (this, ValidationRequestedEventArgs.Empty);
            }
            await Task.WhenAll(tasks);
        }

        // 4. Return validity based on accumulated messages
        return !GetValidationMessages().Any();
    }

    /// <summary>
    /// Registers an async validation task for the specified field.
    /// The task is tracked for pending state queries and cancelled if the field
    /// changes again or the form is submitted.
    /// </summary>
    /// <param name="fieldIdentifier">The field being validated.</param>
    /// <param name="validationTask">The async validation task.</param>
    /// <param name="cancellationTokenSource">
    /// The CancellationTokenSource controlling the task. The framework will cancel it
    /// when the field changes or the form is submitted.
    /// </param>
    public void AddValidationTask(
        in FieldIdentifier fieldIdentifier,
        Task validationTask,
        CancellationTokenSource cancellationTokenSource)
    {
        var fieldState = GetOrAddFieldState(fieldIdentifier);
        fieldState.SetPendingValidationTask(validationTask, cancellationTokenSource);
        NotifyValidationStateChanged();

        // When the task completes, clear pending state and notify UI
        _ = AwaitAndClearPendingTask(fieldIdentifier, fieldState, validationTask);
    }

    /// <summary>
    /// Determines whether the specified field has a pending async validation task.
    /// </summary>
    public bool IsValidationPending(in FieldIdentifier fieldIdentifier)
        => _fieldStates.TryGetValue(fieldIdentifier, out var state) && state.HasPendingValidation;

    /// <summary>
    /// Determines whether the specified field has a pending async validation task.
    /// </summary>
    public bool IsValidationPending(Expression<Func<object>> accessor)
        => IsValidationPending(FieldIdentifier.Create(accessor));

    /// <summary>
    /// Determines whether any field in this <see cref="EditContext"/> has a pending async validation task.
    /// </summary>
    public bool IsValidationPending()
    {
        foreach (var state in _fieldStates)
        {
            if (state.Value.HasPendingValidation)
            {
                return true;
            }
        }
        return false;
    }

    // === PRIVATE HELPERS ===

    private async Task AwaitAndClearPendingTask(
        FieldIdentifier fieldIdentifier, FieldState fieldState, Task validationTask)
    {
        try
        {
            await validationTask;
        }
        catch (OperationCanceledException)
        {
            // Cancellation is expected — silently discard
        }
        finally
        {
            // Only clear if this is still the current task (not replaced by a newer one)
            fieldState.ClearPendingValidationTaskIfCurrent(validationTask);
            NotifyValidationStateChanged();
        }
    }

    private void CancelAllValidationTasks()
    {
        foreach (var state in _fieldStates)
        {
            state.Value.CancelPendingValidation();
        }
    }
}
```

### Key design choices

- **`OnValidationRequestedAsync` uses `Func<object, ValidationRequestedEventArgs, Task>`** rather than a custom delegate type. This mirrors the existing `EventHandler<T>` pattern but returns `Task`. Multiple subscribers are supported via `GetInvocationList()` + `Task.WhenAll()`.
- **`ValidateAsync()` invokes BOTH sync and async handlers.** This ensures backward compatibility — existing sync validators (subscribed to `OnValidationRequested`) still run. Async validators subscribe to the new `OnValidationRequestedAsync`.
- **`AddValidationTask` takes a `CancellationTokenSource`** so the framework can cancel the task. The caller creates the CTS, passes the token to the async work, and passes the CTS here for lifecycle management.
- **`NotifyFieldChanged()` is NOT modified.** The existing sync `OnFieldChanged` event still fires. Cancellation of previous field tasks happens inside the `DataAnnotationsEventSubscriptions.OnFieldChanged` handler (see §3).

---

## 2. FieldState Changes

```csharp
internal sealed class FieldState
{
    // === EXISTING (unchanged) ===
    private readonly FieldIdentifier _fieldIdentifier;
    private HashSet<ValidationMessageStore>? _validationMessageStores;
    public bool IsModified { get; set; }
    // ... existing methods unchanged ...

    // === NEW ===
    private Task? _pendingValidationTask;
    private CancellationTokenSource? _pendingValidationCts;

    /// <summary>
    /// Whether this field has an in-flight async validation task.
    /// </summary>
    public bool HasPendingValidation => _pendingValidationTask is not null
        && !_pendingValidationTask.IsCompleted;

    /// <summary>
    /// Sets the pending validation task for this field.
    /// If a previous task exists, it is cancelled first.
    /// </summary>
    public void SetPendingValidationTask(Task task, CancellationTokenSource cts)
    {
        // Cancel any existing pending validation for this field
        CancelPendingValidation();

        _pendingValidationTask = task;
        _pendingValidationCts = cts;
    }

    /// <summary>
    /// Cancels the pending validation task for this field, if any.
    /// </summary>
    public void CancelPendingValidation()
    {
        if (_pendingValidationCts is not null)
        {
            _pendingValidationCts.Cancel();
            _pendingValidationCts.Dispose();
            _pendingValidationCts = null;
        }
        _pendingValidationTask = null;
    }

    /// <summary>
    /// Clears the pending validation state only if the specified task is still the current one.
    /// This prevents a completed stale task from clearing a newer task's pending state.
    /// </summary>
    public void ClearPendingValidationTaskIfCurrent(Task task)
    {
        if (_pendingValidationTask == task)
        {
            _pendingValidationCts?.Dispose();
            _pendingValidationCts = null;
            _pendingValidationTask = null;
        }
    }
}
```

### Key design choices

- **One task per field.** A field can only have one pending async validation at a time. Starting a new one cancels the previous one. This matches the user mental model (field has one value → one validation in progress).
- **`ClearPendingValidationTaskIfCurrent` uses reference equality** to guard against a race where a stale task completes after a newer task has been registered.

---

## 3. EditContextDataAnnotationsExtensions Changes

This is the largest change. The `DataAnnotationsEventSubscriptions` class needs to:
1. Subscribe to `OnValidationRequestedAsync` for form-submit async validation
2. Start async field validation on `OnFieldChanged` and register via `AddValidationTask`
3. Implement debounce for field-level async validation

```csharp
private sealed partial class DataAnnotationsEventSubscriptions : IDisposable
{
    // === EXISTING FIELDS (unchanged) ===
    private static readonly ConcurrentDictionary<(Type ModelType, string FieldName), PropertyInfo?> _propertyInfoCache = new();
    private readonly EditContext _editContext;
    private readonly IServiceProvider? _serviceProvider;
    private readonly ValidationMessageStore _messages;
    private readonly ValidationOptions? _validationOptions;
    private readonly IValidatableInfo? _validatorTypeInfo;
    private readonly Dictionary<string, FieldIdentifier> _validationPathToFieldIdentifierMapping = new();

    // === NEW FIELDS ===
    private readonly ValidationMessageStore _asyncFieldMessages;

    // === MODIFIED CONSTRUCTOR ===
    public DataAnnotationsEventSubscriptions(EditContext editContext, IServiceProvider serviceProvider)
    {
        _editContext = editContext ?? throw new ArgumentNullException(nameof(editContext));
        _serviceProvider = serviceProvider;
        _messages = new ValidationMessageStore(_editContext);
        _asyncFieldMessages = new ValidationMessageStore(_editContext);
        _validationOptions = _serviceProvider?.GetService<IOptions<ValidationOptions>>()?.Value;
        _validatorTypeInfo = _validationOptions is not null
            && _validationOptions.TryGetValidatableTypeInfo(_editContext.Model.GetType(), out var typeInfo)
            ? typeInfo : null;

        _editContext.OnFieldChanged += OnFieldChanged;
        _editContext.OnValidationRequested += OnValidationRequested;
        _editContext.OnValidationRequestedAsync += OnValidationRequestedAsync; // NEW

        if (MetadataUpdater.IsSupported)
        {
            OnClearCache += ClearCache;
        }
    }

    // === MODIFIED: OnFieldChanged — now also starts async field validation ===
    private void OnFieldChanged(object? sender, FieldChangedEventArgs eventArgs)
    {
        var fieldIdentifier = eventArgs.FieldIdentifier;
        if (TryGetValidatableProperty(fieldIdentifier, out var propertyInfo))
        {
            var propertyValue = propertyInfo.GetValue(fieldIdentifier.Model);
            var validationContext = new ValidationContext(fieldIdentifier.Model, _serviceProvider, items: null)
            {
                MemberName = propertyInfo.Name
            };

            // --- Sync validation (unchanged) ---
            var results = new List<ValidationResult>();
            Validator.TryValidateProperty(propertyValue, validationContext, results);
            _messages.Clear(fieldIdentifier);
            foreach (var result in CollectionsMarshal.AsSpan(results))
            {
                _messages.Add(fieldIdentifier, result.ErrorMessage!);
            }

            // --- Async field validation (NEW) ---
            // Clear previous async results for this field
            _asyncFieldMessages.Clear(fieldIdentifier);

            // Only start async validation if sync validation passed for this field
            if (!_messages[fieldIdentifier].Any())
            {
                StartAsyncFieldValidation(fieldIdentifier, propertyValue, validationContext);
            }

            _editContext.NotifyValidationStateChanged();
        }
    }

    // === NEW: Start async validation for a single field ===
    private void StartAsyncFieldValidation(
        FieldIdentifier fieldIdentifier,
        object? propertyValue,
        ValidationContext validationContext)
    {
        var cts = new CancellationTokenSource();
        var task = RunAsyncFieldValidationAsync(fieldIdentifier, propertyValue, validationContext, cts.Token);
        _editContext.AddValidationTask(fieldIdentifier, task, cts);
    }

    private async Task RunAsyncFieldValidationAsync(
        FieldIdentifier fieldIdentifier,
        object? propertyValue,
        ValidationContext validationContext,
        CancellationToken cancellationToken)
    {
        // NOTE: Validator.TryValidatePropertyAsync does not exist yet in the BCL.
        // This call represents the future async counterpart to Validator.TryValidateProperty.
        // Until the BCL adds this API (tracked in dotnet/runtime#121536), this will need
        // a temporary implementation or polyfill.
        var results = new List<ValidationResult>();
        await Validator.TryValidatePropertyAsync(propertyValue, validationContext, results, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        _asyncFieldMessages.Clear(fieldIdentifier);
        foreach (var result in CollectionsMarshal.AsSpan(results))
        {
            _asyncFieldMessages.Add(fieldIdentifier, result.ErrorMessage!);
        }

        _editContext.NotifyValidationStateChanged();
    }

    // === EXISTING: OnValidationRequested (sync, unchanged) ===
    private void OnValidationRequested(object? sender, ValidationRequestedEventArgs e)
    {
        var validationContext = new ValidationContext(_editContext.Model, _serviceProvider, items: null);

        // Only run sync path here — async is handled by OnValidationRequestedAsync
        ValidateWithDefaultValidator(validationContext);

        _editContext.NotifyValidationStateChanged();
    }

    // === NEW: OnValidationRequestedAsync — async form-submit validation ===
    private async Task OnValidationRequestedAsync(object sender, ValidationRequestedEventArgs e)
    {
        if (_validatorTypeInfo is null)
        {
            return;
        }

        var validationContext = new ValidationContext(_editContext.Model, _serviceProvider, items: null);

        var validateContext = new ValidateContext
        {
            ValidationOptions = _validationOptions!,
            ValidationContext = validationContext,
        };

        try
        {
            validateContext.OnValidationError += AddMapping;
            await _validatorTypeInfo.ValidateAsync(
                _editContext.Model, validateContext, CancellationToken.None);
        }
        finally
        {
            validateContext.OnValidationError -= AddMapping;
        }

        var validationErrors = validateContext.ValidationErrors;

        // Transfer results to the ValidationMessageStore
        _messages.Clear();
        _asyncFieldMessages.Clear();

        if (validationErrors is not null && validationErrors.Count > 0)
        {
            foreach (var (fieldKey, messages) in validationErrors)
            {
                if (_validationPathToFieldIdentifierMapping.TryGetValue(fieldKey, out var fieldIdentifier))
                {
                    _messages.Add(fieldIdentifier, messages);
                }
            }
        }

        _validationPathToFieldIdentifierMapping.Clear();

        _editContext.NotifyValidationStateChanged();
    }

    // === MODIFIED: TryValidateTypeInfo — remove async throw guard ===
    private bool TryValidateTypeInfo(ValidationContext validationContext)
    {
        if (_validatorTypeInfo is null)
        {
            return false;
        }

        // In the sync Validate() path, we still call ValidateAsync but only accept
        // synchronously-completing results. If it doesn't complete synchronously,
        // the validation infrastructure will throw (per Decision D2).
        var validateContext = new ValidateContext
        {
            ValidationOptions = _validationOptions!,
            ValidationContext = validationContext,
        };
        try
        {
            validateContext.OnValidationError += AddMapping;
            var validationTask = _validatorTypeInfo.ValidateAsync(
                _editContext.Model, validateContext, CancellationToken.None);
            if (!validationTask.IsCompleted)
            {
                throw new InvalidOperationException(
                    "This model contains async validators. Use EditContext.ValidateAsync() instead of Validate().");
            }

            var validationErrors = validateContext.ValidationErrors;
            _messages.Clear();
            if (validationErrors is not null && validationErrors.Count > 0)
            {
                foreach (var (fieldKey, messages) in validationErrors)
                {
                    _messages.Add(_validationPathToFieldIdentifierMapping[fieldKey], messages);
                }
            }
        }
        finally
        {
            validateContext.OnValidationError -= AddMapping;
            _validationPathToFieldIdentifierMapping.Clear();
        }

        return true;
    }

    // === MODIFIED: Dispose — unsubscribe from async event too ===
    public void Dispose()
    {
        _messages.Clear();
        _asyncFieldMessages.Clear();
        _editContext.OnFieldChanged -= OnFieldChanged;
        _editContext.OnValidationRequested -= OnValidationRequested;
        _editContext.OnValidationRequestedAsync -= OnValidationRequestedAsync;
        _editContext.NotifyValidationStateChanged();

        if (MetadataUpdater.IsSupported)
        {
            OnClearCache -= ClearCache;
        }
    }

    // ... remaining existing methods (ValidateWithDefaultValidator, TryGetValidatableProperty,
    //     AddMapping, ClearCache) unchanged ...
}
```

### Key design choices

- **Separate `ValidationMessageStore` for async field results** (`_asyncFieldMessages`). This keeps sync and async results independent — sync results are stored in `_messages` (as today), async field results in `_asyncFieldMessages`. Both are cleared appropriately.
- **Sync-first optimization**: Async field validation only starts if sync validation passed for that field. No point checking uniqueness if `[Required]` already failed.
- **`OnValidationRequested` (sync) still runs `ValidateWithDefaultValidator`.** The sync handler handles the legacy `Validator.TryValidateObject` path. The async handler (`OnValidationRequestedAsync`) handles the M.E.Validation path. Both are invoked by `ValidateAsync()` (per decision D6 — keep both paths).
- **Error message is improved** from `"Async validation is not supported"` to `"This model contains async validators. Use EditContext.ValidateAsync() instead of Validate()."`.

---

## 4. EditForm Changes

```csharp
public class EditForm : ComponentBase
{
    // === EXISTING (unchanged) ===
    // ... all parameters, OnParametersSet, BuildRenderTree unchanged ...

    // === MODIFIED: HandleSubmitAsync — now uses ValidateAsync ===
    private async Task HandleSubmitAsync()
    {
        Debug.Assert(_editContext != null);

        if (OnSubmit.HasDelegate)
        {
            // When using OnSubmit, the developer takes control of the validation lifecycle
            await OnSubmit.InvokeAsync(_editContext);
        }
        else
        {
            // Use async validation — awaits both sync and async validators
            var isValid = await _editContext.ValidateAsync();

            if (isValid && OnValidSubmit.HasDelegate)
            {
                await OnValidSubmit.InvokeAsync(_editContext);
            }

            if (!isValid && OnInvalidSubmit.HasDelegate)
            {
                await OnInvalidSubmit.InvokeAsync(_editContext);
            }
        }
    }

    // BuildRenderTree is unchanged from current implementation — no form-level CSS class added.
    // Developers who want a form-level "validating" class can use
    // editContext.IsValidationPending() in their own markup.
}
```

### Key design choices

- **`HandleSubmitAsync()` calls `ValidateAsync()` instead of `Validate()`.** This is the single most important change — it replaces the sync `Validate()` call with `ValidateAsync()`. Since `ValidateAsync()` also invokes sync handlers, existing `DataAnnotationsValidator` subscriptions still work.
- **`BuildRenderTree` is unchanged.** No form-level CSS class is added (Decision D5 revised). Developers who want a form-level indicator can check `editContext.IsValidationPending()` in their own markup.

---

## 5. FieldCssClassProvider Changes

```csharp
public class FieldCssClassProvider
{
    internal static readonly FieldCssClassProvider Instance = new FieldCssClassProvider();

    public virtual string GetFieldCssClass(EditContext editContext, in FieldIdentifier fieldIdentifier)
    {
        // NEW: Check pending state first
        if (editContext.IsValidationPending(fieldIdentifier))
        {
            if (editContext.IsModified(fieldIdentifier))
            {
                return "modified pending";
            }
            else
            {
                return "pending";
            }
        }

        var isValid = !editContext.GetValidationMessages(fieldIdentifier).Any();
        if (editContext.IsModified(fieldIdentifier))
        {
            return isValid ? "modified valid" : "modified invalid";
        }
        else
        {
            return isValid ? "valid" : "invalid";
        }
    }
}
```

### Key design choices

- **`"pending"` CSS class** follows the existing pattern (`"valid"`, `"invalid"`, `"modified valid"`, `"modified invalid"`). Now adds `"pending"` and `"modified pending"`.
- **Pending takes priority** over valid/invalid. While async validation is running, the field is neither valid nor invalid yet — it's pending. Once validation completes, it transitions to valid or invalid.
- **Custom `FieldCssClassProvider` subclasses** can override this method to return their own classes (e.g., `"is-validating"` for Bootstrap), using the same `editContext.IsValidationPending(fieldIdentifier)` API.

---

## 6. InputBase Changes

```csharp
public abstract class InputBase<TValue> : ComponentBase, IDisposable
{
    // === EXISTING (unchanged) ===
    // ... all existing parameters and logic ...

    // === NEW: Debounce parameter ===

    /// <summary>
    /// Gets or sets the debounce duration for async validation on this field.
    /// When set, async validation will not start until the user stops changing
    /// the field value for the specified duration.
    /// If null, async validation triggers immediately on field change.
    /// </summary>
    [Parameter] public TimeSpan? ValidationDebounce { get; set; }
}
```

The actual debounce logic is implemented in `DataAnnotationsEventSubscriptions.OnFieldChanged` — `InputBase` just carries the parameter. The debounce value is retrieved from the component via the `EditContext.Properties` bag or a new mechanism.

**Open issue**: How does `DataAnnotationsEventSubscriptions` read the `ValidationDebounce` value from `InputBase`? Options:
1. `InputBase` writes `ValidationDebounce` to `EditContext.Properties` keyed by `FieldIdentifier` when the parameter changes
2. A new `FieldOptions` concept on `EditContext` stores per-field configuration
3. The debounce is implemented entirely inside `InputBase` (delays the `NotifyFieldChanged` call)

Option 3 is simplest — `InputBase` itself delays calling `EditContext.NotifyFieldChanged()` by the debounce duration. This keeps the debounce logic self-contained:

```csharp
// Inside InputBase<TValue>, in CurrentValue setter:
protected TValue? CurrentValue
{
    get => Value;
    set
    {
        var hasChanged = !EqualityComparer<TValue>.Default.Equals(value, Value);
        if (hasChanged)
        {
            _parsingFailed = false;
            Value = value;
            _ = ValueChanged.InvokeAsync(Value);

            if (ValidationDebounce is { } debounce && debounce > TimeSpan.Zero)
            {
                DebounceFieldChanged(debounce);
            }
            else
            {
                EditContext?.NotifyFieldChanged(FieldIdentifier);
            }
        }
    }
}

private CancellationTokenSource? _debounceCts;

private async void DebounceFieldChanged(TimeSpan debounce)
{
    // Cancel previous debounce
    _debounceCts?.Cancel();
    _debounceCts?.Dispose();
    _debounceCts = new CancellationTokenSource();
    var token = _debounceCts.Token;

    try
    {
        await Task.Delay(debounce, token);
        EditContext?.NotifyFieldChanged(FieldIdentifier);
    }
    catch (OperationCanceledException)
    {
        // Debounce was cancelled by a newer value change — expected
    }
}
```

---

## 7. ValidationPendingIndicator Component (New)

```csharp
namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Displays content when async validation is pending for the specified field.
/// The content is hidden when validation completes.
/// </summary>
public class ValidationPendingIndicator<TValue> : ComponentBase, IDisposable
{
    private EditContext? _previousEditContext;
    private FieldIdentifier _fieldIdentifier;
    private EventHandler<ValidationStateChangedEventArgs>? _validationStateChangedHandler;

    /// <summary>
    /// Specifies the field for which pending validation state should be monitored.
    /// </summary>
    [Parameter] public Expression<Func<TValue>>? For { get; set; }

    /// <summary>
    /// The content to display when validation is pending.
    /// If not specified, a default "Validating..." text is rendered.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    [CascadingParameter] private EditContext CurrentEditContext { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (CurrentEditContext is null)
        {
            throw new InvalidOperationException(
                $"{GetType()} requires a cascading parameter of type {nameof(EditContext)}. " +
                $"For example, you can use {GetType()} inside an {nameof(EditForm)}.");
        }

        if (For is null)
        {
            throw new InvalidOperationException(
                $"{GetType()} requires a value for the {nameof(For)} parameter.");
        }

        if (CurrentEditContext != _previousEditContext)
        {
            DetachValidationStateChangedListener();
            _previousEditContext = CurrentEditContext;
            _validationStateChangedHandler = (sender, args) => StateHasChanged();
            CurrentEditContext.OnValidationStateChanged += _validationStateChangedHandler;
        }

        _fieldIdentifier = FieldIdentifier.Create(For);
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (CurrentEditContext.IsValidationPending(_fieldIdentifier))
        {
            if (ChildContent is not null)
            {
                builder.AddContent(0, ChildContent);
            }
            else
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "validation-pending");
                builder.AddContent(2, "Validating...");
                builder.CloseElement();
            }
        }
    }

    private void DetachValidationStateChangedListener()
    {
        if (_previousEditContext is not null && _validationStateChangedHandler is not null)
        {
            _previousEditContext.OnValidationStateChanged -= _validationStateChangedHandler;
        }
    }

    public void Dispose()
    {
        DetachValidationStateChangedListener();
    }
}
```

### Key design choices

- **Generic type parameter** `<TValue>` matches the pattern used by `ValidationMessage<TValue>` — the `For` parameter uses `Expression<Func<TValue>>` for field identification.
- **`ChildContent`** allows custom rendering (spinners, icons, etc.). Default is a `<span class="validation-pending">Validating...</span>`.
- **Re-renders on `OnValidationStateChanged`** — same mechanism used by `ValidationMessage` and `InputBase`.

---

## 8. Complete Flow Diagrams

### Form Submit Flow (with async)

```
User clicks Submit
  │
  ▼
EditForm.HandleSubmitAsync()
  │
  ├─ OnSubmit.HasDelegate?
  │    YES → await OnSubmit.InvokeAsync(_editContext)  [developer controls]
  │    NO  ↓
  │
  ▼
await _editContext.ValidateAsync()
  │
  ├─ 1. CancelAllValidationTasks()          ← cancels any in-flight field validations
  ├─ 2. OnValidationRequested?.Invoke()      ← sync handlers (legacy path)
  │      └─ DataAnnotationsEventSubscriptions.OnValidationRequested()
  │           └─ Validator.TryValidateObject() or TryValidateTypeInfo() [sync]
  ├─ 3. await Task.WhenAll(OnValidationRequestedAsync handlers)
  │      └─ DataAnnotationsEventSubscriptions.OnValidationRequestedAsync()
  │           └─ await _validatorTypeInfo.ValidateAsync()  [truly async now!]
  ├─ 4. return !GetValidationMessages().Any()
  │
  ▼
isValid?
  YES → await OnValidSubmit.InvokeAsync()
  NO  → await OnInvalidSubmit.InvokeAsync()
```

### Field Change Flow (with async)

```
User edits field (blur/change)
  │
  ▼
InputBase.CurrentValue setter
  │
  ├─ ValidationDebounce set?
  │    YES → delay, then NotifyFieldChanged()
  │    NO  → NotifyFieldChanged() immediately
  │
  ▼
EditContext.NotifyFieldChanged(fieldIdentifier)
  │
  ▼
OnFieldChanged?.Invoke()
  │
  ▼
DataAnnotationsEventSubscriptions.OnFieldChanged()
  │
  ├─ 1. Sync: Validator.TryValidateProperty()     ← runs immediately
  ├─ 2. Store sync results in _messages
  ├─ 3. Sync passed? Start async validation:
  │      ├─ Create CancellationTokenSource
  │      ├─ Start RunAsyncFieldValidationAsync()
  │      └─ EditContext.AddValidationTask(field, task, cts)
  │           ├─ FieldState.SetPendingValidationTask()  ← cancels previous if any
  │           └─ NotifyValidationStateChanged()          ← UI shows "pending"
  ├─ 4. NotifyValidationStateChanged()
  │
  ▼
UI re-renders:
  - InputBase: CssClass = "modified pending"
  - ValidationPendingIndicator: renders spinner
  - Submit button: disabled (IsValidationPending())

  ... async validation completes ...

  ▼
RunAsyncFieldValidationAsync completes:
  ├─ Store async results in _asyncFieldMessages
  ├─ FieldState.ClearPendingValidationTaskIfCurrent()
  └─ NotifyValidationStateChanged()

  ▼
UI re-renders:
  - InputBase: CssClass = "modified valid" or "modified invalid"
  - ValidationPendingIndicator: renders nothing
  - ValidationMessage: shows errors (if any)
```

---

## 9. Files Changed Summary

| File | Type | Changes |
|---|---|---|
| `src/Components/Forms/src/EditContext.cs` | Modified | Add `ValidateAsync()`, `OnValidationRequestedAsync`, `AddValidationTask()`, `IsValidationPending()`, `CancelAllValidationTasks()` |
| `src/Components/Forms/src/FieldState.cs` | Modified | Add `HasPendingValidation`, `SetPendingValidationTask()`, `CancelPendingValidation()`, `ClearPendingValidationTaskIfCurrent()` |
| `src/Components/Forms/src/EditContextDataAnnotationsExtensions.cs` | Modified | Subscribe to `OnValidationRequestedAsync`, add `_asyncFieldMessages`, `StartAsyncFieldValidation()`, `RunAsyncFieldValidationAsync()`, `OnValidationRequestedAsync()`, improved error message in `TryValidateTypeInfo` |
| `src/Components/Web/src/Forms/EditForm.cs` | Modified | `HandleSubmitAsync()` calls `ValidateAsync()` |
| `src/Components/Web/src/Forms/FieldCssClassProvider.cs` | Modified | Add `"pending"` / `"modified pending"` CSS classes |
| `src/Components/Web/src/Forms/InputBase.cs` | Modified | Add `ValidationDebounce` parameter, debounce logic in `CurrentValue` setter |
| `src/Components/Web/src/Forms/ValidationPendingIndicator.cs` | **New** | Component for displaying pending validation state |

### Public API additions

```csharp
// EditContext
public event Func<object, ValidationRequestedEventArgs, Task>? OnValidationRequestedAsync;
public Task<bool> ValidateAsync();
public void AddValidationTask(in FieldIdentifier fieldIdentifier, Task validationTask, CancellationTokenSource cancellationTokenSource);
public bool IsValidationPending(in FieldIdentifier fieldIdentifier);
public bool IsValidationPending(Expression<Func<object>> accessor);
public bool IsValidationPending();

// InputBase<TValue>
[Parameter] public TimeSpan? ValidationDebounce { get; set; }

// New type
public class ValidationPendingIndicator<TValue> : ComponentBase, IDisposable
```

---

## 10. Open Issues for Implementation

1. **Debounce and `NotifyFieldChanged` timing**: The debounce in `InputBase` delays `NotifyFieldChanged`. This means the field's `IsModified` state and two-way binding (`ValueChanged`) fire immediately, but validation is delayed. Is this the right UX?

2. **`Validator.TryValidatePropertyAsync` does not exist yet**: The field-level async validation path uses a theoretical `Validator.TryValidatePropertyAsync` API that does not exist in the BCL today. This is tracked in [dotnet/runtime#121536](https://github.com/dotnet/runtime/issues/121536). Until this API ships, we will need a temporary polyfill or an alternative implementation path.

3. **Static SSR forms**: In SSR mode, `HandleSubmitAsync()` is called during HTTP POST processing. `ValidateAsync()` will work naturally here — it awaits async validators, then the response includes validation errors. No special handling needed beyond what's already designed.

4. **Thread safety**: `EditContext` is not designed for concurrent access (Blazor components run on a single sync context). The `AddValidationTask` / `CancelPendingValidation` operations are safe as long as they're called from the Blazor sync context, which they will be.
