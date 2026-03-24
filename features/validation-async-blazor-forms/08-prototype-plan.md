# Async Form Validation — Prototype Design & Implementation Plan

## 1. Overview

This document describes the design and implementation plan for a prototype of async validation support in Blazor forms, based on the [spec (07-spec-draft-v4.md)](./07-spec-draft-v4.md). The prototype targets the core async validation pipeline: form-submit async validation, field-level async validation with pending/faulted state, cancellation, and CSS class feedback. It is scoped to prove out the architecture and API shape, not to ship as a complete feature.

### Prototype scope

| In scope | Deferred |
|----------|----------|
| `EditContext.ValidateAsync()` | `ValidationMessage` PendingContent/FaultedContent render fragments |
| `OnValidationRequestedAsync` event | `ValidateFieldAsync(FieldIdentifier)` retry API |
| `EditContext.AddValidationTask()` with cancellation | SSR streaming of pending state |
| `IsValidationPending()` / `IsValidationFaulted()` | Complex object graph async validation via `Microsoft.Extensions.Validation` |
| `EditForm` → `ValidateAsync()` integration | Debouncing |
| `DataAnnotationsValidator` async event wiring | |
| `FieldCssClassProvider` pending/faulted CSS classes | |
| Cancellation on field re-edit and on form submit | |
| Mark `Validate()` as `[Obsolete]` | |

### Design decision: `Validate()` deprecation

We use **Alternative A** from the spec: `Validate()` is marked `[Obsolete]` pointing to `ValidateAsync()`. During the prototype, `Validate()` continues to work for sync-only validators (unchanged behavior) but emits a compiler warning. This gives a clear migration signal without a runtime behavior change.

---

## 2. Existing Architecture Summary

The current validation pipeline is entirely synchronous:

```
EditForm.HandleSubmitAsync()
  → EditContext.Validate()
    → raises OnValidationRequested (sync EventHandler)
      → DataAnnotationsEventSubscriptions.OnValidationRequested()
        → Validator.TryValidateObject() or IValidatableInfo.ValidateAsync() [sync-asserted]
        → populates ValidationMessageStore
        → calls EditContext.NotifyValidationStateChanged()
    → returns !GetValidationMessages().Any()
  → OnValidSubmit / OnInvalidSubmit
```

**Key files and their roles:**

| File | Assembly | Role |
|------|----------|------|
| `src/Components/Forms/src/EditContext.cs` | Forms | Core state: events, `Validate()`, field states, message queries |
| `src/Components/Forms/src/FieldState.cs` | Forms | Per-field state: `IsModified`, associated `ValidationMessageStore`s |
| `src/Components/Forms/src/ValidationMessageStore.cs` | Forms | Stores validation messages per field |
| `src/Components/Forms/src/EditContextDataAnnotationsExtensions.cs` | Forms | `DataAnnotationsEventSubscriptions` — wires `OnFieldChanged` / `OnValidationRequested` |
| `src/Components/Forms/src/DataAnnotationsValidator.cs` | Forms | Component that calls `EnableDataAnnotationsValidation()` |
| `src/Components/Web/src/Forms/EditForm.cs` | Web | Renders `<form>`, calls `Validate()` in `HandleSubmitAsync()` |
| `src/Components/Web/src/Forms/InputBase.cs` | Web | Base input; gets CSS from `FieldCssClassProvider`, listens to `OnValidationStateChanged` |
| `src/Components/Web/src/Forms/FieldCssClassProvider.cs` | Web | Returns `"modified valid"` / `"modified invalid"` / `"valid"` / `"invalid"` |
| `src/Components/Web/src/Forms/ValidationMessage.cs` | Web | Renders `<div class="validation-message">` per error |
| `src/Components/Web/src/Forms/ValidationSummary.cs` | Web | Renders `<ul>` with all errors |

**Important constraints:**
- `EditContext` is accessed from a single synchronization context (Blazor's renderer). No concurrent access safeguards needed.
- `FieldState` is `internal` — we can freely add async tracking fields.
- `EditContextDataAnnotationsExtensions.DataAnnotationsEventSubscriptions` already calls `IValidatableInfo.ValidateAsync()` but **asserts it completes synchronously** (line 164–167: `if (!validationTask.IsCompleted) throw`). This is the exact point we need to make truly async.
- `EditForm.HandleSubmitAsync()` is already an `async Task`, so changing `Validate()` → `await ValidateAsync()` is straightforward.
- `FieldCssClassProvider.GetFieldCssClass()` is `virtual`, so derived classes already override it. Adding pending/faulted requires changes to the base implementation.

---

## 3. API Design

### 3.1 New APIs on `EditContext`

```csharp
public sealed class EditContext
{
    // --- Existing (modified) ---

    [Obsolete("Use ValidateAsync() instead.")]
    public bool Validate() { ... } // Unchanged behavior, now obsolete

    // --- New: Async validation event ---

    /// <summary>
    /// An async event raised when validation is requested. Validators subscribe to this
    /// to perform async validation (e.g., database lookups, remote API calls).
    /// All handlers are invoked and awaited by <see cref="ValidateAsync"/>.
    /// </summary>
    public event Func<object, ValidationRequestedEventArgs, Task>? OnValidationRequestedAsync;

    /// <summary>
    /// Validates this <see cref="EditContext"/> asynchronously.
    /// Invokes both sync <see cref="OnValidationRequested"/> and async
    /// <see cref="OnValidationRequestedAsync"/> handlers, cancels any pending
    /// field-level validation tasks, then returns whether the model is valid.
    /// </summary>
    public async Task<bool> ValidateAsync() { ... }

    // --- New: Field-level async task tracking ---

    /// <summary>
    /// Registers an async validation task for a specific field. The task is tracked
    /// for pending/faulted state queries. If a task is already tracked for this field,
    /// the previous task's <paramref name="cts"/> is cancelled and the new task replaces it.
    /// </summary>
    public void AddValidationTask(
        in FieldIdentifier fieldIdentifier,
        Task task,
        CancellationTokenSource cts);

    // --- New: State queries ---

    /// <summary>
    /// Returns <c>true</c> if any field has a pending async validation task.
    /// </summary>
    public bool IsValidationPending();

    /// <summary>
    /// Returns <c>true</c> if the specified field has a pending async validation task.
    /// </summary>
    public bool IsValidationPending(in FieldIdentifier fieldIdentifier);

    /// <summary>
    /// Returns <c>true</c> if the specified field's last async validation faulted.
    /// </summary>
    public bool IsValidationFaulted(in FieldIdentifier fieldIdentifier);

    /// <summary>
    /// Returns <c>true</c> if any field's async validation has faulted.
    /// </summary>
    public bool IsValidationFaulted();
}
```

**Event type rationale:** `OnValidationRequestedAsync` uses `Func<object, ValidationRequestedEventArgs, Task>` rather than `EventHandler<>` because `EventHandler` is `void`-returning. We need to `await` all subscribers. The invocation iterates the delegate list, invokes each, and awaits all returned tasks (see §4.1).

### 3.2 Async Validation Task State Machine (per field)

Each field can be in one of these async validation states:

```
[None] ──(AddValidationTask)──▶ [Pending]
                                    │
                    ┌───────────────┼───────────────┐
                    ▼               ▼               ▼
               [Completed]    [Cancelled]      [Faulted]
               (clear state)  (clear state)    (sticky until
                                                re-edit or
                                                form submit)
```

- **None** → no tracked async task. Default.
- **Pending** → `AddValidationTask` was called, task is not yet completed. `IsValidationPending` returns `true`.
- **Completed** → Task completed successfully. State cleared to None. Validation messages already in `ValidationMessageStore`.
- **Cancelled** → Task was cancelled (field re-edited or form submitted). State cleared to None. Silent.
- **Faulted** → Task threw a non-cancellation exception. `IsValidationFaulted` returns `true`. Sticky until field is re-edited or form submit re-validates.

### 3.3 `FieldCssClassProvider` changes

The default `FieldCssClassProvider.GetFieldCssClass()` adds `"pending"` or `"faulted"` to the compound CSS class string:

```csharp
public virtual string GetFieldCssClass(EditContext editContext, in FieldIdentifier fieldIdentifier)
{
    if (editContext.IsValidationPending(fieldIdentifier))
    {
        return editContext.IsModified(fieldIdentifier) ? "modified pending" : "pending";
    }

    if (editContext.IsValidationFaulted(fieldIdentifier))
    {
        return editContext.IsModified(fieldIdentifier) ? "modified faulted" : "faulted";
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
```

Pending/faulted take priority over valid/invalid — a field with a pending async task should not show "valid" even if there are no current messages.

### 3.4 `EditForm` changes

`HandleSubmitAsync()` changes from:
```csharp
var isValid = _editContext.Validate();
```
to:
```csharp
var isValid = await _editContext.ValidateAsync();
```

This is the only change to `EditForm`. The method is already `async Task`.

### 3.5 `DataAnnotationsEventSubscriptions` changes

The `DataAnnotationsEventSubscriptions` class (inside `EditContextDataAnnotationsExtensions`) is extended:

1. **Subscribe to `OnValidationRequestedAsync`** in addition to `OnValidationRequested`.
2. **In `OnValidationRequestedAsync` handler:** Call `IValidatableInfo.ValidateAsync()` with a real `await` (remove the `if (!task.IsCompleted) throw` guard).
3. **In `OnFieldChanged` handler:** When a field has async validators, start async validation, register via `AddValidationTask()`, and let the continuation populate the `ValidationMessageStore` and call `NotifyValidationStateChanged()`.

For the prototype, field-level async detection uses the `IValidatableInfo` path (the `Microsoft.Extensions.Validation` pipeline, which already knows about async validators). The legacy `Validator.TryValidateProperty` path remains sync-only — async field validation for the legacy path requires BCL `Validator.TryValidatePropertyAsync` which doesn't exist yet.

---

## 4. Internal Design Details

### 4.1 `ValidateAsync()` implementation

```csharp
public async Task<bool> ValidateAsync()
{
    // 1. Cancel all pending field-level async tasks (form-submit supersedes them)
    CancelAllPendingValidationTasks();

    // 2. Fire sync event (backward compat — existing validators listen here)
    OnValidationRequested?.Invoke(this, ValidationRequestedEventArgs.Empty);

    // 3. Fire async event — invoke all handlers and await
    if (OnValidationRequestedAsync is { } asyncHandler)
    {
        var delegates = asyncHandler.GetInvocationList();
        var tasks = new Task[delegates.Length];
        for (var i = 0; i < delegates.Length; i++)
        {
            tasks[i] = ((Func<object, ValidationRequestedEventArgs, Task>)delegates[i])
                .Invoke(this, ValidationRequestedEventArgs.Empty);
        }
        await Task.WhenAll(tasks);
    }

    // 4. Return validity
    return !GetValidationMessages().Any();
}
```

Key points:
- Sync `OnValidationRequested` fires first (existing validators produce their messages).
- Async `OnValidationRequestedAsync` fires next; all handlers run concurrently via `Task.WhenAll`.
- Field-level pending tasks are cancelled before full-form validation runs (per spec: "form-submit validation supersedes field-level tasks").

### 4.2 `AddValidationTask()` + tracking on `FieldState`

Async validation state is tracked on the internal `FieldState` class:

```csharp
internal sealed class FieldState
{
    // Existing
    public bool IsModified { get; set; }
    private HashSet<ValidationMessageStore>? _validationMessageStores;

    // New: async validation tracking
    internal Task? PendingValidationTask { get; set; }
    internal CancellationTokenSource? PendingValidationCts { get; set; }
    internal bool IsFaulted { get; set; }
}
```

`EditContext.AddValidationTask()` implementation:

```csharp
public void AddValidationTask(in FieldIdentifier fieldIdentifier, Task task, CancellationTokenSource cts)
{
    var state = GetOrAddFieldState(fieldIdentifier);

    // Cancel any previous pending task for this field
    if (state.PendingValidationCts is { } previousCts)
    {
        previousCts.Cancel();
        previousCts.Dispose();
    }

    state.PendingValidationTask = task;
    state.PendingValidationCts = cts;
    state.IsFaulted = false;

    NotifyValidationStateChanged(); // trigger UI update (shows pending CSS)

    // Observe the task's completion to update state
    _ = ObserveValidationTaskAsync(fieldIdentifier, state, task);
}

private async Task ObserveValidationTaskAsync(FieldIdentifier fieldIdentifier, FieldState state, Task task)
{
    try
    {
        await task;
    }
    catch (OperationCanceledException)
    {
        // Cancellation is silent — field was re-edited or form submitted
    }
    catch (Exception)
    {
        // Infrastructure fault — mark field as faulted
        if (ReferenceEquals(state.PendingValidationTask, task))
        {
            state.IsFaulted = true;
        }
    }
    finally
    {
        // Only clear if this is still the current task (not replaced by a newer one)
        if (ReferenceEquals(state.PendingValidationTask, task))
        {
            state.PendingValidationTask = null;
            state.PendingValidationCts?.Dispose();
            state.PendingValidationCts = null;
            NotifyValidationStateChanged(); // trigger UI update
        }
    }
}
```

**Identity check via `ReferenceEquals`:** If the user re-edits a field, `AddValidationTask` is called again with a new task. The old task's continuation must not overwrite the new task's state. The `ReferenceEquals(state.PendingValidationTask, task)` guard ensures only the most recent task updates state.

### 4.3 Cancellation on field re-edit

When `NotifyFieldChanged` is called for a field that has a pending async validation task, the previous task should be cancelled. This is handled in two places:

1. **`AddValidationTask`** cancels the previous CTS when registering a new task.
2. **`DataAnnotationsEventSubscriptions.OnFieldChanged`** creates a new CTS for each field-level validation and registers it via `AddValidationTask`.

No changes to `NotifyFieldChanged` itself — the `OnFieldChanged` handler in `DataAnnotationsEventSubscriptions` handles the async lifecycle.

### 4.4 Cancellation on form submit

`ValidateAsync()` calls `CancelAllPendingValidationTasks()` before running full-form validation:

```csharp
private void CancelAllPendingValidationTasks()
{
    foreach (var (_, state) in _fieldStates)
    {
        if (state.PendingValidationCts is { } cts)
        {
            cts.Cancel();
            cts.Dispose();
            state.PendingValidationTask = null;
            state.PendingValidationCts = null;
            state.IsFaulted = false;
        }
    }
}
```

### 4.5 `OnValidationRequestedAsync` handler in `DataAnnotationsEventSubscriptions`

```csharp
private async Task OnValidationRequestedAsync(object sender, ValidationRequestedEventArgs e)
{
    if (_validatorTypeInfo is null)
    {
        return; // Fall back to sync path (legacy Validator)
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
        await _validatorTypeInfo.ValidateAsync(_editContext.Model, validateContext, CancellationToken.None);

        var validationErrors = validateContext.ValidationErrors;
        _messages.Clear();

        if (validationErrors is not null && validationErrors.Count > 0)
        {
            foreach (var (fieldKey, messages) in validationErrors)
            {
                var fieldIdentifier = _validationPathToFieldIdentifierMapping[fieldKey];
                _messages.Add(fieldIdentifier, messages);
            }
        }
    }
    finally
    {
        validateContext.OnValidationError -= AddMapping;
        _validationPathToFieldIdentifierMapping.Clear();
    }

    _editContext.NotifyValidationStateChanged();
}
```

This replaces `TryValidateTypeInfo()` for the async submit path. The sync `OnValidationRequested` handler continues to work for backward compatibility with the legacy `Validator.TryValidateObject` path.

---

## 5. Implementation Plan

### Phase 1: Core async pipeline on `EditContext` (Forms assembly)

**Task 1.1 — Add async validation tracking to `FieldState`**
- File: `src/Components/Forms/src/FieldState.cs`
- Add `PendingValidationTask`, `PendingValidationCts`, `IsFaulted` properties

**Task 1.2 — Add async APIs to `EditContext`**
- File: `src/Components/Forms/src/EditContext.cs`
- Add `OnValidationRequestedAsync` event
- Add `ValidateAsync()` method
- Add `AddValidationTask()` method
- Add `IsValidationPending()` overloads
- Add `IsValidationFaulted()` overloads
- Add `CancelAllPendingValidationTasks()` (private)
- Add `ObserveValidationTaskAsync()` (private)
- Mark `Validate()` with `[Obsolete]`
- Update `PublicAPI.Unshipped.txt`

### Phase 2: DataAnnotations async support (Forms assembly)

**Task 2.1 — Add async event subscription in `DataAnnotationsEventSubscriptions`**
- File: `src/Components/Forms/src/EditContextDataAnnotationsExtensions.cs`
- Subscribe to `OnValidationRequestedAsync` in constructor
- Implement `OnValidationRequestedAsync` handler: truly await `IValidatableInfo.ValidateAsync()`
- Remove the `if (!validationTask.IsCompleted) throw` guard (make it the async path)
- Keep sync `OnValidationRequested` for the legacy `Validator.TryValidateObject` fallback
- In `OnFieldChanged`: detect async-capable fields, start async validation, register via `AddValidationTask`
- Unsubscribe from `OnValidationRequestedAsync` in `Dispose()`

### Phase 3: EditForm + CSS integration (Web assembly)

**Task 3.1 — Update `EditForm.HandleSubmitAsync()` to use `ValidateAsync()`**
- File: `src/Components/Web/src/Forms/EditForm.cs`
- Change `_editContext.Validate()` → `await _editContext.ValidateAsync()`

**Task 3.2 — Update `FieldCssClassProvider` with pending/faulted classes**
- File: `src/Components/Web/src/Forms/FieldCssClassProvider.cs`
- Add pending/faulted checks before valid/invalid logic

### Phase 4: Tests

**Task 4.1 — `EditContext` unit tests**
- File: `src/Components/Forms/test/EditContextTest.cs`
- `ValidateAsync` invokes both sync and async handlers
- `AddValidationTask` tracks pending state
- Cancellation on re-edit clears pending, does not fault
- Faulted state on task exception
- `IsValidationPending` / `IsValidationFaulted` per-field and form-level
- `ValidateAsync` cancels pending field tasks before running
- `Validate()` still works for sync-only validators

**Task 4.2 — `DataAnnotationsEventSubscriptions` tests**
- File: `src/Components/Forms/test/EditContextDataAnnotationsExtensionsTest.cs`
- Async form-submit validation via `ValidateAsync`
- Field-level async validation lifecycle

**Task 4.3 — `FieldCssClassProvider` tests**
- Pending and faulted CSS class generation

---

## 6. File Change Summary

| File | Change type | Description |
|------|-------------|-------------|
| `src/Components/Forms/src/FieldState.cs` | Modify | Add async tracking fields |
| `src/Components/Forms/src/EditContext.cs` | Modify | Add `ValidateAsync`, `AddValidationTask`, state queries, obsolete `Validate` |
| `src/Components/Forms/src/EditContextDataAnnotationsExtensions.cs` | Modify | Async event wiring, true async `ValidateAsync` path, field-level async in `OnFieldChanged` |
| `src/Components/Forms/src/DataAnnotationsValidator.cs` | No change | Delegates to `EnableDataAnnotationsValidation`, no changes needed |
| `src/Components/Forms/src/PublicAPI.Unshipped.txt` | Modify | Add new public API entries |
| `src/Components/Web/src/Forms/EditForm.cs` | Modify | `Validate()` → `await ValidateAsync()` |
| `src/Components/Web/src/Forms/FieldCssClassProvider.cs` | Modify | Add pending/faulted CSS logic |
| `src/Components/Web/src/Forms/InputBase.cs` | No change | Already re-renders on `OnValidationStateChanged`; CSS comes from provider |
| `src/Components/Web/src/Forms/ValidationMessage.cs` | No change (deferred) | Render fragments deferred from prototype |
| `src/Components/Forms/test/EditContextTest.cs` | Modify | Add async validation tests |
| `src/Components/Forms/test/EditContextDataAnnotationsExtensionsTest.cs` | Modify | Add async validation tests |

---

## 7. Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| `IValidatableInfo.ValidateAsync` may not support per-field async validation yet | Prototype field-level async only for types registered via `AddValidation()`. Legacy `Validator.TryValidateProperty` stays sync. Document this limitation. |
| `async void` hazard if third-party code subscribes to `OnFieldChanged` and starts fire-and-forget async work | `AddValidationTask` is the official mechanism. Document that `async void` handlers are unsupported. |
| `ObserveValidationTaskAsync` captures a fire-and-forget task via `_ = ...` | The task is tracked via `FieldState.PendingValidationTask` and observed for exceptions. The discard is intentional — we don't need to await it from `AddValidationTask`. |
| Breaking change: `FieldCssClassProvider` subclasses may not expect pending/faulted inputs | The base implementation changes are additive. Custom overrides that don't call `base` are unaffected since they don't call `IsValidationPending`/`IsValidationFaulted`. |
| `[Obsolete]` on `Validate()` generates warnings for all existing consumers | This is intentional as a migration signal. The fix is trivial: `Validate()` → `await ValidateAsync()`. |

---

## 8. Open Questions (Deferred from Prototype)

These are documented in the spec and deferred from the prototype scope:

1. **`ValidationMessage` render fragments (PendingContent / FaultedContent)** — Requires changing `ValidationMessage` from flat-div rendering to supporting `RenderFragment` children. May be a breaking DOM change. Deferred to post-prototype design review.

2. **`ValidateFieldAsync(FieldIdentifier)` retry API** — Useful for retry-after-fault UI. Semantically different from `NotifyFieldChanged` (which marks modified). Deferred — developers can use `NotifyFieldChanged` as a workaround in the prototype.

3. **SSR streaming of pending state** — Technically possible with `Enhance` forms but adds complexity. Deferred.

4. **`OnValidationRequestedAsync` handler exceptions during form submit** — In the prototype, exceptions propagate out of `ValidateAsync()` and surface as unhandled in `EditForm.HandleSubmitAsync()`. A form-level "faulted" state is deferred.

5. **Complex object graph async validation** — Depends on `Microsoft.Extensions.Validation` async traversal working correctly for nested objects. The prototype wires the plumbing; deep graph testing is deferred.
