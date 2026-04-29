// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Holds metadata related to a data editing process, such as flags to indicate which
/// fields have been modified and the current set of validation messages.
/// </summary>
public sealed class EditContext
{
    // Note that EditContext tracks state for any FieldIdentifier you give to it, plus
    // the underlying storage is sparse. As such, none of the APIs have a "field not found"
    // error state. If you give us an unrecognized FieldIdentifier, that just means we
    // didn't yet track any state for it, so we behave as if it's in the default state
    // (valid and unmodified).
    private readonly Dictionary<FieldIdentifier, FieldState> _fieldStates = new Dictionary<FieldIdentifier, FieldState>();
    private bool _isFormValidationFaulted;
    private bool _isFormValidationPending;

    /// <summary>
    /// Constructs an instance of <see cref="EditContext"/>.
    /// </summary>
    /// <param name="model">The model object for the <see cref="EditContext"/>. This object should hold the data being edited, for example as a set of properties.</param>
    public EditContext(object model)
    {
        // The only reason we disallow null is because you'd almost always want one, and if you
        // really don't, you can pass an empty object then ignore it. Ensuring it's nonnull
        // simplifies things for all consumers of EditContext.
        Model = model ?? throw new ArgumentNullException(nameof(model));
        Properties = new EditContextProperties();
    }

    /// <summary>
    /// An event that is raised when a field value changes.
    /// </summary>
    public event EventHandler<FieldChangedEventArgs>? OnFieldChanged;

    /// <summary>
    /// An event that is raised when validation is requested. Validator components subscribe
    /// to this event to perform synchronous validation.
    /// </summary>
    /// <remarks>
    /// For a given source of validation, a validator component should subscribe to either
    /// <see cref="OnValidationRequested"/> or <see cref="OnValidationRequestedAsync"/>, not
    /// both. Subscribing to both causes the same validator to run twice on
    /// <see cref="ValidateAsync"/>, with the second pass overwriting the first.
    /// (The built-in <see cref="EditContextDataAnnotationsExtensions"/> validator subscribes
    /// to both intentionally and routes between sync- and async-only execution internally.)
    /// </remarks>
    public event EventHandler<ValidationRequestedEventArgs>? OnValidationRequested;

    /// <summary>
    /// An async event that is raised when validation is requested. Validator components
    /// subscribe to this event to perform async validation (e.g., database lookups, remote API calls).
    /// Handlers are awaited by <see cref="ValidateAsync"/>. <see cref="Validate"/> also invokes
    /// these handlers but requires each to complete synchronously; if any returns an incomplete
    /// <see cref="Task"/>, <see cref="Validate"/> throws <see cref="InvalidOperationException"/>.
    /// </summary>
    /// <remarks>
    /// For a given source of validation, a validator component should subscribe to either
    /// <see cref="OnValidationRequested"/> or <see cref="OnValidationRequestedAsync"/>, not
    /// both. See <see cref="OnValidationRequested"/> for details.
    /// </remarks>
    public event Func<object, ValidationRequestedEventArgs, Task>? OnValidationRequestedAsync;

    /// <summary>
    /// An event that is raised when validation state has changed.
    /// </summary>
    public event EventHandler<ValidationStateChangedEventArgs>? OnValidationStateChanged;

    /// <summary>
    /// Supplies a <see cref="FieldIdentifier"/> corresponding to a specified field name
    /// on this <see cref="EditContext"/>'s <see cref="Model"/>.
    /// </summary>
    /// <param name="fieldName">The name of the editable field.</param>
    /// <returns>A <see cref="FieldIdentifier"/> corresponding to a specified field name on this <see cref="EditContext"/>'s <see cref="Model"/>.</returns>
    public FieldIdentifier Field(string fieldName)
        => new FieldIdentifier(Model, fieldName);

    /// <summary>
    /// Gets the model object for this <see cref="EditContext"/>.
    /// </summary>
    public object Model { get; }

    /// <summary>
    /// Gets a collection of arbitrary properties associated with this instance.
    /// </summary>
    public EditContextProperties Properties { get; }

    /// <summary>
    /// Gets whether field identifiers should be generated for &lt;input&gt; elements.
    /// </summary>
    public bool ShouldUseFieldIdentifiers { get; set; } = !OperatingSystem.IsBrowser();

    /// <summary>
    /// Signals that the value for the specified field has changed.
    /// </summary>
    /// <param name="fieldIdentifier">Identifies the field whose value has been changed.</param>
    public void NotifyFieldChanged(in FieldIdentifier fieldIdentifier)
    {
        GetOrAddFieldState(fieldIdentifier).IsModified = true;
        OnFieldChanged?.Invoke(this, new FieldChangedEventArgs(fieldIdentifier));
    }

    /// <summary>
    /// Signals that some aspect of validation state has changed.
    /// </summary>
    public void NotifyValidationStateChanged()
    {
        OnValidationStateChanged?.Invoke(this, ValidationStateChangedEventArgs.Empty);
    }

    /// <summary>
    /// Clears any modification flag that may be tracked for the specified field.
    /// </summary>
    /// <param name="fieldIdentifier">Identifies the field whose modification flag (if any) should be cleared.</param>
    public void MarkAsUnmodified(in FieldIdentifier fieldIdentifier)
    {
        if (_fieldStates.TryGetValue(fieldIdentifier, out var state))
        {
            state.IsModified = false;
        }
    }

    /// <summary>
    /// Clears all modification flags within this <see cref="EditContext"/>.
    /// </summary>
    public void MarkAsUnmodified()
    {
        foreach (var state in _fieldStates)
        {
            state.Value.IsModified = false;
        }
    }

    /// <summary>
    /// Determines whether any of the fields in this <see cref="EditContext"/> have been modified.
    /// </summary>
    /// <returns>True if any of the fields in this <see cref="EditContext"/> have been modified; otherwise false.</returns>
    public bool IsModified()
    {
        // If necessary, we could consider caching the overall "is modified" state and only recomputing
        // when there's a call to NotifyFieldModified/NotifyFieldUnmodified
        foreach (var state in _fieldStates)
        {
            if (state.Value.IsModified)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the current validation messages across all fields.
    ///
    /// This method does not perform validation itself. It only returns messages determined by previous validation actions.
    /// </summary>
    /// <returns>The current validation messages.</returns>
    public IEnumerable<string> GetValidationMessages()
    {
        // Since we're only enumerating the fields for which we have a non-null state, the cost of this grows
        // based on how many fields have been modified or have associated validation messages
        foreach (var state in _fieldStates)
        {
            foreach (var message in state.Value.GetValidationMessages())
            {
                yield return message;
            }
        }
    }

    /// <summary>
    /// Gets the current validation messages for the specified field.
    ///
    /// This method does not perform validation itself. It only returns messages determined by previous validation actions.
    /// </summary>
    /// <param name="fieldIdentifier">Identifies the field whose current validation messages should be returned.</param>
    /// <returns>The current validation messages for the specified field.</returns>
    public IEnumerable<string> GetValidationMessages(FieldIdentifier fieldIdentifier)
    {
        if (_fieldStates.TryGetValue(fieldIdentifier, out var state))
        {
            foreach (var message in state.GetValidationMessages())
            {
                yield return message;
            }
        }
    }

    /// <summary>
    /// Gets the current validation messages for the specified field.
    ///
    /// This method does not perform validation itself. It only returns messages determined by previous validation actions.
    /// </summary>
    /// <param name="accessor">Identifies the field whose current validation messages should be returned.</param>
    /// <returns>The current validation messages for the specified field.</returns>
    public IEnumerable<string> GetValidationMessages(Expression<Func<object>> accessor)
        => GetValidationMessages(FieldIdentifier.Create(accessor));

    /// <summary>
    /// Determines whether the specified fields in this <see cref="EditContext"/> has been modified.
    /// </summary>
    /// <returns>True if the field has been modified; otherwise false.</returns>
    public bool IsModified(in FieldIdentifier fieldIdentifier)
        => _fieldStates.TryGetValue(fieldIdentifier, out var state)
        ? state.IsModified
        : false;

    /// <summary>
    /// Determines whether the specified fields in this <see cref="EditContext"/> has been modified.
    /// </summary>
    /// <param name="accessor">Identifies the field whose current validation messages should be returned.</param>
    /// <returns>True if the field has been modified; otherwise false.</returns>
    public bool IsModified(Expression<Func<object>> accessor)
        => IsModified(FieldIdentifier.Create(accessor));

    /// <summary>
    /// Determines whether the specified fields in this <see cref="EditContext"/> has no associated validation messages.
    /// </summary>
    /// <returns>True if the field has no associated validation messages after validation; otherwise false.</returns>
    public bool IsValid(in FieldIdentifier fieldIdentifier)
        => !GetValidationMessages(fieldIdentifier).Any();

    /// <summary>
    /// Determines whether the specified fields in this <see cref="EditContext"/> has no associated validation messages.
    /// </summary>
    /// <param name="accessor">Identifies the field whose current validation messages should be returned.</param>
    /// <returns>True if the field has no associated validation messages after validation; otherwise false.</returns>
    public bool IsValid(Expression<Func<object>> accessor)
        => IsValid(FieldIdentifier.Create(accessor));

    /// <summary>
    /// Validates this <see cref="EditContext"/>.
    /// </summary>
    /// <returns>True if there are no validation messages after validation; otherwise false.</returns>
    /// <remarks>
    /// Validation must not be re-entered. Do not call <see cref="Validate"/> or
    /// <see cref="ValidateAsync"/> from inside an <see cref="OnValidationRequested"/>,
    /// <see cref="OnValidationRequestedAsync"/>, or <see cref="OnValidationStateChanged"/>
    /// handler attached to the same <see cref="EditContext"/>; doing so produces undefined
    /// behavior (in particular, it can cause infinite recursion via the validator's own
    /// <see cref="NotifyValidationStateChanged"/> calls).
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when an <see cref="OnValidationRequestedAsync"/> handler does not complete synchronously.
    /// Use <see cref="ValidateAsync"/> instead when async validators are registered.
    /// </exception>
    public bool Validate()
    {
        var faultedThisPass = false;

        OnValidationRequested?.Invoke(this, ValidationRequestedEventArgs.Empty);

        if (OnValidationRequestedAsync is { } asyncHandler)
        {
            var delegates = asyncHandler.GetInvocationList();
            for (var i = 0; i < delegates.Length; i++)
            {
                var task = ((Func<object, ValidationRequestedEventArgs, Task>)delegates[i])
                    .Invoke(this, ValidationRequestedEventArgs.Empty);

                if (!task.IsCompleted)
                {
                    throw new InvalidOperationException(
                        $"An asynchronous validation handler did not complete synchronously. " +
                        $"Use {nameof(ValidateAsync)} instead of {nameof(Validate)} when async validators are registered.");
                }

                // Surface synchronous faults. GetResult unwraps single exceptions (no AggregateException).
                task.GetAwaiter().GetResult();
            }
        }

        // Synchronous pass completed normally. Clear any prior form-level fault since this
        // pass observed a fresh non-faulted result.
        _isFormValidationFaulted = faultedThisPass;

        return !GetValidationMessages().Any();
    }

    /// <summary>
    /// Validates this <see cref="EditContext"/> asynchronously.
    /// Cancels any pending field-level async validation tasks, invokes the synchronous
    /// <see cref="OnValidationRequested"/> handlers, then invokes and awaits the asynchronous
    /// <see cref="OnValidationRequestedAsync"/> handlers concurrently. Exceptions from synchronous
    /// handlers propagate to the caller, matching <see cref="Validate"/>. Any non-cancellation
    /// exception thrown by an asynchronous handler is contained: the form is marked as faulted
    /// (observable via the parameterless <see cref="IsValidationFaulted()"/>) and the method
    /// returns <c>false</c>.
    /// While the asynchronous portion is in flight, the parameterless
    /// <see cref="IsValidationPending()"/> returns <c>true</c> so applications can show a global
    /// "validating..." indicator without wrapping the call themselves. The form-level
    /// <see cref="IsValidationFaulted()"/> result is updated only when a pass completes; it is
    /// preserved across caller-cancelled passes.
    /// </summary>
    /// <param name="cancellationToken">A token that signals cancellation of this validation pass.
    /// The token is exposed to async handlers via <see cref="ValidationRequestedEventArgs.CancellationToken"/>.
    /// If the caller cancels the token, this method throws <see cref="OperationCanceledException"/>;
    /// the form is not marked as faulted in that case and the previous form-level fault state is preserved.
    /// The token bounds the in-flight pass only; field-level validation tasks that start independently
    /// during the awaited window (for example, from user edits) are not linked to this token and
    /// continue running.</param>
    /// <returns>True if there are no validation messages after validation and no async handler
    /// faulted; otherwise false.</returns>
    /// <remarks>
    /// Validation must not be re-entered. Do not call <see cref="Validate"/> or
    /// <see cref="ValidateAsync"/> from inside an <see cref="OnValidationRequested"/>,
    /// <see cref="OnValidationRequestedAsync"/>, or <see cref="OnValidationStateChanged"/>
    /// handler attached to the same <see cref="EditContext"/>; doing so produces undefined
    /// behavior.
    /// </remarks>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/>
    /// is cancelled before or during the validation pass.</exception>
    public async Task<bool> ValidateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Cancel all pending field-level async tasks - form-submit validation supersedes them
        CancelAllPendingValidationTasks();

        // Mark the pass as in-flight so apps can show a global "validating..." state via
        // IsValidationPending() without wrapping this call. Notify so subscribers re-render.
        _isFormValidationPending = true;
        NotifyValidationStateChanged();

        var faultedThisPass = false;

        try
        {
            // Sync handlers run unchanged - their exceptions propagate to the caller, matching Validate().
            // This guarantees observable parity for apps that haven't introduced any async validators.
            OnValidationRequested?.Invoke(this, ValidationRequestedEventArgs.Empty);

            if (OnValidationRequestedAsync is { } asyncHandler)
            {
                // Only allocate a new args instance when the caller actually supplied a cancellable token;
                // otherwise reuse the shared Empty instance with None token.
                var args = cancellationToken.CanBeCanceled
                    ? new ValidationRequestedEventArgs(cancellationToken)
                    : ValidationRequestedEventArgs.Empty;

                var delegates = asyncHandler.GetInvocationList();
                var tasks = new Task[delegates.Length];

                for (var i = 0; i < delegates.Length; i++)
                {
                    try
                    {
                        tasks[i] = ((Func<object, ValidationRequestedEventArgs, Task>)delegates[i])
                            .Invoke(this, args)
                            ?? Task.CompletedTask;
                    }
                    catch (Exception ex)
                    {
                        // Sync throw before the handler's first await - normalize to a faulted Task
                        // so all handlers are observed uniformly via Task.WhenAll.
                        tasks[i] = Task.FromException(ex);
                    }
                }

                try
                {
                    await Task.WhenAll(tasks);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Caller-requested cancellation propagates to the caller. The previous form-level
                    // fault state is preserved since no new result was produced. The finally block
                    // still clears the pending flag.
                    throw;
                }
                catch (OperationCanceledException)
                {
                    // Handler-internal cancellation (not the caller's token) is silently contained.
                }
                catch
                {
                    faultedThisPass = true;
                }

                // Task.WhenAll surfaces only one exception; if cancellation won the race we may
                // still have other faulted tasks. Inspect the array to catch those.
                if (!faultedThisPass)
                {
                    for (var i = 0; i < tasks.Length; i++)
                    {
                        if (tasks[i].IsFaulted)
                        {
                            faultedThisPass = true;
                            break;
                        }
                    }
                }
            }

            // Pass completed (success, invalid, or contained handler fault). Assign the new fault
            // result atomically so that consumers querying IsValidationFaulted() during the pass
            // continue to see the previous result instead of a brief reset-then-set flicker.
            _isFormValidationFaulted = faultedThisPass;
        }
        finally
        {
            // Pending flag flips to false unconditionally - whether we completed normally,
            // were cancelled by the caller, or a sync handler threw. Single end-of-pass
            // notification covers both the pending change and any fault assignment above.
            _isFormValidationPending = false;
            NotifyValidationStateChanged();
        }

        return !faultedThisPass && !GetValidationMessages().Any();
    }

    /// <summary>
    /// Registers an async validation task for a specific field. The task is tracked for
    /// pending/faulted state queries via <see cref="IsValidationPending(in FieldIdentifier)"/>
    /// and <see cref="IsValidationFaulted(in FieldIdentifier)"/>. If a task is already tracked
    /// for this field, the previous task's <paramref name="cts"/> is cancelled and the new task replaces it.
    /// The <see cref="EditContext"/> takes ownership of the supplied <paramref name="cts"/>: it will be
    /// cancelled if a subsequent validation supersedes this one, and disposed once <paramref name="task"/> completes.
    /// </summary>
    /// <remarks>
    /// If <paramref name="task"/> is already completed, it is settled synchronously: the field is
    /// not parked in the pending state, a faulted task is surfaced via
    /// <see cref="IsValidationFaulted(in FieldIdentifier)"/>, and <paramref name="cts"/> is disposed.
    /// <para>
    /// Validators backing <paramref name="task"/> are expected to clear any prior validation
    /// messages for the field up-front (before awaiting), and to avoid writing partial results
    /// to a <see cref="ValidationMessageStore"/> on a path that may subsequently throw. If a
    /// validator does write partial state and then throws, those messages remain in the store
    /// until cleared by a subsequent successful validation.
    /// </para>
    /// </remarks>
    /// <param name="fieldIdentifier">Identifies the field being validated.</param>
    /// <param name="task">The async validation task to track.</param>
    /// <param name="cts">The <see cref="CancellationTokenSource"/> that can cancel the task.</param>
    public void AddValidationTask(in FieldIdentifier fieldIdentifier, Task task, CancellationTokenSource cts)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(cts);

        var state = GetOrAddFieldState(fieldIdentifier);

        // Cancel any previous pending task for this field. Its observer disposes its own CTS
        // when the validator task settles, so we must not dispose it here while it may still be in flight.
        state.PendingValidationCts?.Cancel();

        if (task.IsCompleted)
        {
            // Settle synchronously without parking the slot. Mirrors observer policy: only set
            // IsValidationFaulted on a faulted task; cancel and success are no-ops on the fault flag.
            if (task.IsFaulted)
            {
                _ = task.Exception; // observe to suppress UnobservedTaskException
                if (!state.IsValidationFaulted)
                {
                    state.IsValidationFaulted = true;
                    NotifyValidationStateChanged();
                }
            }
            cts.Dispose();
            return;
        }

        state.PendingValidationTask = task;
        state.PendingValidationCts = cts;
        state.IsValidationFaulted = false;

        NotifyValidationStateChanged();

        // Observe the task's completion to update state and dispose the CTS.
        _ = ObserveValidationTaskAsync(state, task, cts);
    }

    /// <summary>
    /// Returns <c>true</c> if the specified field has a pending async validation task.
    /// </summary>
    /// <param name="fieldIdentifier">Identifies the field to query.</param>
    /// <returns><c>true</c> if async validation is in progress for the field; otherwise <c>false</c>.</returns>
    public bool IsValidationPending(in FieldIdentifier fieldIdentifier)
        => _fieldStates.TryGetValue(fieldIdentifier, out var state) && state.PendingValidationTask is { IsCompleted: false };

    /// <summary>
    /// Returns <c>true</c> if the field identified by the <paramref name="accessor"/> expression
    /// has a pending async validation task.
    /// </summary>
    /// <typeparam name="TField">The type of the field.</typeparam>
    /// <param name="accessor">An expression that identifies the field, e.g. <c>() =&gt; model.Email</c>.</param>
    /// <returns><c>true</c> if async validation is in progress for the field; otherwise <c>false</c>.</returns>
    public bool IsValidationPending<TField>(Expression<Func<TField>> accessor)
        => IsValidationPending(FieldIdentifier.Create(accessor));

    /// <summary>
    /// Returns <c>true</c> if a form-level <see cref="ValidateAsync"/> pass is currently in flight.
    /// Suitable for driving form-wide UI such as disabling a submit button or showing a
    /// "validating..." indicator for the current submission. Does not consider field-level pending
    /// tasks (those are superseded when the next form-level pass starts); use the
    /// <see cref="IsValidationPending(in FieldIdentifier)"/> overload for per-field state.
    /// </summary>
    /// <returns><c>true</c> if a form-level validation pass is in progress; otherwise <c>false</c>.</returns>
    public bool IsValidationPending() => _isFormValidationPending;

    /// <summary>
    /// Returns <c>true</c> if the specified field's last async validation faulted
    /// (threw a non-cancellation exception).
    /// </summary>
    /// <param name="fieldIdentifier">Identifies the field to query.</param>
    /// <returns><c>true</c> if the field's last async validation faulted; otherwise <c>false</c>.</returns>
    public bool IsValidationFaulted(in FieldIdentifier fieldIdentifier)
        => _fieldStates.TryGetValue(fieldIdentifier, out var state) && state.IsValidationFaulted;

    /// <summary>
    /// Returns <c>true</c> if the field identified by the <paramref name="accessor"/> expression's
    /// last async validation faulted (threw a non-cancellation exception).
    /// </summary>
    /// <typeparam name="TField">The type of the field.</typeparam>
    /// <param name="accessor">An expression that identifies the field, e.g. <c>() =&gt; model.Email</c>.</param>
    /// <returns><c>true</c> if the field's last async validation faulted; otherwise <c>false</c>.</returns>
    public bool IsValidationFaulted<TField>(Expression<Func<TField>> accessor)
        => IsValidationFaulted(FieldIdentifier.Create(accessor));

    /// <summary>
    /// Returns <c>true</c> if the most recent <see cref="ValidateAsync"/> pass observed an
    /// unhandled exception from any <see cref="OnValidationRequestedAsync"/> handler. Use this
    /// to detect that validation itself failed (as opposed to producing validation messages).
    /// For per-field validator faults from <see cref="AddValidationTask"/>, use the
    /// <see cref="IsValidationFaulted(in FieldIdentifier)"/> overload.
    /// </summary>
    /// <returns><c>true</c> if the most recent form-level validation pass faulted; otherwise <c>false</c>.</returns>
    public bool IsValidationFaulted() => _isFormValidationFaulted;

    internal FieldState? GetFieldState(in FieldIdentifier fieldIdentifier)
    {
        _fieldStates.TryGetValue(fieldIdentifier, out var state);
        return state;
    }

    internal FieldState GetOrAddFieldState(in FieldIdentifier fieldIdentifier)
    {
        if (!_fieldStates.TryGetValue(fieldIdentifier, out var state))
        {
            state = new FieldState(fieldIdentifier);
            _fieldStates.Add(fieldIdentifier, state);
        }

        return state;
    }

    private void CancelAllPendingValidationTasks()
    {
        var changed = false;
        foreach (var (_, state) in _fieldStates)
        {
            if (state.PendingValidationCts is { } cts)
            {
                // Request cancellation; the observer task disposes the CTS when the validator settles.
                cts.Cancel();
                state.PendingValidationTask = null;
                state.PendingValidationCts = null;
                state.IsValidationFaulted = false;
                changed = true;
            }
            else if (state.IsValidationFaulted)
            {
                // Field had a previously-faulted task that already settled (so its CTS was
                // already cleared). Clear the lingering fault flag at the start of the new
                // validation pass so per-field IsValidationFaulted reflects only the current
                // pass's outcome.
                state.IsValidationFaulted = false;
                changed = true;
            }
        }

        if (changed)
        {
            // The pending/faulted state of one or more fields just changed. Notify so UI components
            // observing IsValidationPending or IsValidationFaulted re-render even if no subsequent
            // handler raises its own notification.
            NotifyValidationStateChanged();
        }
    }

    private async Task ObserveValidationTaskAsync(FieldState state, Task task, CancellationTokenSource cts)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            // Cancellation is silent - field was re-edited or form submitted
        }
        catch (Exception)
        {
            // Infrastructure fault - mark field as faulted only if this is still the current task
            if (ReferenceEquals(state.PendingValidationTask, task))
            {
                state.IsValidationFaulted = true;
            }
        }
        finally
        {
            // Only clear slot state if this is still the current task (not replaced by a newer one)
            if (ReferenceEquals(state.PendingValidationTask, task))
            {
                state.PendingValidationTask = null;
                state.PendingValidationCts = null;
                NotifyValidationStateChanged();
            }

            // Always dispose the CTS we own. Safe whether or not the slot is still current,
            // and only happens after the validator task has observably settled, so the validator
            // can never observe a disposed token.
            cts.Dispose();
        }
    }
}
