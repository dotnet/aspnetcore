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
    private bool _lastFormValidationFaulted;
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
    /// to this event to perform validation.
    /// </summary>
    /// <remarks>
    /// Handlers run synchronously in subscription order. A validator that performs asynchronous work
    /// keeps its handler synchronous and registers a factory via
    /// <see cref="ValidationRequestedEventArgs.AddValidationTask(Func{CancellationToken, Task})"/>, for example
    /// <c>(sender, args) =&gt; args.AddValidationTask(token =&gt; ValidateAsyncCore(token))</c>.
    /// <see cref="ValidateAsync(CancellationToken)"/> invokes every registered factory and awaits the
    /// resulting tasks; <see cref="Validate"/> throws <see cref="InvalidOperationException"/> if a handler
    /// registers asynchronous work. Do not use an <c>async void</c> handler: its work is not awaited and
    /// is silently skipped.
    /// </remarks>
    public event EventHandler<ValidationRequestedEventArgs>? OnValidationRequested;

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
    /// Validates this <see cref="EditContext"/> synchronously.
    /// </summary>
    /// <returns><c>true</c> if there are no validation messages after validation; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// Runs synchronous validators only. A handler that registers asynchronous work via
    /// <see cref="ValidationRequestedEventArgs.AddValidationTask(Func{CancellationToken, Task})"/> causes an
    /// <see cref="InvalidOperationException"/>; use <see cref="ValidateAsync"/> for async validators.
    /// Do not call validation re-entrantly from a validation handler.
    /// </remarks>
    /// <exception cref="InvalidOperationException">A handler registered an asynchronous validation task.</exception>
    public bool Validate()
    {
        // Sync pass: args.IsAsync is false, so AddValidationTask throws. A handler exception propagates.
        OnValidationRequested?.Invoke(this, new ValidationRequestedEventArgs());

        // A clean sync pass produced no infrastructure fault.
        _lastFormValidationFaulted = false;

        return !GetValidationMessages().Any();
    }

    /// <summary>
    /// Validates this <see cref="EditContext"/> asynchronously, awaiting any validation tasks that handlers
    /// register via <see cref="ValidationRequestedEventArgs.AddValidationTask(Func{CancellationToken, Task})"/>.
    /// </summary>
    /// <param name="cancellationToken">Cancels this validation pass; it is passed to each registered factory.
    /// If cancelled (including already-cancelled on entry) this method throws <see cref="OperationCanceledException"/>
    /// once the registered tasks settle, and the previous faulted state is preserved.</param>
    /// <returns><c>true</c> if there are no validation messages and no registered task faulted; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// Pending field-level validations are superseded. While tasks are in flight the parameterless
    /// <see cref="IsValidationPending()"/> is <c>true</c>. A non-cancellation exception on a registered task is
    /// contained (the form is marked faulted via <see cref="IsValidationFaulted()"/> and the method returns
    /// <c>false</c>) but not surfaced. Do not call validation re-entrantly from a handler.
    /// </remarks>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
    public async Task<bool> ValidateAsync(CancellationToken cancellationToken = default)
    {
        var args = new ValidationRequestedEventArgs { IsAsync = true };

        // Form submission supersedes any in-flight per-field async validation, and the form is shown
        // as pending for the duration of this pass.
        CancelAllPendingValidationTasks();
        _isFormValidationPending = true;
        NotifyValidationStateChanged();

        try
        {
            // A synchronous handler exception is captured into the returned task. Handlers only register
            // factories during the raise; none have been invoked yet, so a throw leaves no work started.
            OnValidationRequested?.Invoke(this, args);

            // Invoke all registered factories to start their work concurrently.
            var factories = args.ValidationTaskFactories;
            var tasks = new Task[factories.Count];
            for (var i = 0; i < tasks.Length; i++)
            {
                tasks[i] = InvokeValidationFactory(factories[i], cancellationToken);
            }

            var faulted = false;
            foreach (var task in tasks)
            {
                try
                {
                    await task;
                }
                catch
                {
                    // The validation faulted, or was cancelled by a source other than the caller's token.
                    // A caller-cancelled task is discarded below when ThrowIfCancellationRequested throws.
                    faulted = true;
                }
            }

            // If the token is cancelled, the form is not marked as faulted
            // and the previous form-level faulted state is preserved.
            cancellationToken.ThrowIfCancellationRequested();

            _lastFormValidationFaulted = faulted;
            return !faulted && !GetValidationMessages().Any();
        }
        finally
        {
            _isFormValidationPending = false;
            NotifyValidationStateChanged();
        }

        static Task InvokeValidationFactory(Func<CancellationToken, Task> validate, CancellationToken cancellationToken)
        {
            Task? task;
            try
            {
                task = validate(cancellationToken);
            }
            catch (Exception ex)
            {
                // A factory that throws synchronously is contained as a faulted task.
                return Task.FromException(ex);
            }

            // A factory that returns a null task is an application bug, so we throw to the ValidateAsync caller.
            return task ?? throw new ArgumentNullException(nameof(validate), "The validation factory returned a null task.");
        }
    }

    /// <summary>
    /// Registers an asynchronous validation for a specific field. The <paramref name="validate"/> factory
    /// is invoked immediately with a <see cref="CancellationToken"/> owned by this <see cref="EditContext"/>,
    /// and the returned <see cref="Task"/> is tracked for <see cref="IsValidationPending(in FieldIdentifier)"/>
    /// and <see cref="IsValidationFaulted(in FieldIdentifier)"/>. A validation already tracked for the field
    /// is cancelled and replaced.
    /// </summary>
    /// <remarks>
    /// The <see cref="EditContext"/> owns the token source end to end; to also cancel from another source,
    /// link inside <paramref name="validate"/> via
    /// <see cref="CancellationTokenSource.CreateLinkedTokenSource(CancellationToken)"/>. Write
    /// <paramref name="validate"/> as an <c>async</c> method so a pre-<c>await</c> throw is captured into the
    /// task rather than thrown from this method. If <paramref name="validate"/> throws or returns
    /// <see langword="null"/>, any prior validation for the field is still superseded before the exception
    /// propagates. Validators should clear prior messages for the field up-front and avoid writing partial
    /// results on a path that may throw.
    /// </remarks>
    /// <param name="fieldIdentifier">Identifies the field being validated.</param>
    /// <param name="validate">A factory that starts the asynchronous validation using the supplied
    /// cancellation token and returns the in-flight <see cref="Task"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="validate"/> is <see langword="null"/>,
    /// or it returned a <see langword="null"/> task.</exception>
    public void TrackFieldValidation(in FieldIdentifier fieldIdentifier, Func<CancellationToken, Task> validate)
    {
        ArgumentNullException.ThrowIfNull(validate);

        var state = GetOrAddFieldState(fieldIdentifier);

        // EditContext owns the token source: cancelled when a later validation supersedes this one, and
        // disposed once the task settles (both handled by the observer).
        var cts = new CancellationTokenSource();
        Task task;
        try
        {
            task = validate(cts.Token);
        }
        catch
        {
            // The new validation failed to start, so the observer will not run. Clear the prior
            // validation so the field is not left stuck pending with an outdated validator in flight.
            cts.Dispose();
            ClearPendingFieldValidation(state);
            throw;
        }

        if (task is null)
        {
            cts.Dispose();
            ClearPendingFieldValidation(state);
            throw new ArgumentNullException(nameof(validate), "The validation factory returned a null task.");
        }

        // Supersession, settle, notification, and CTS disposal all flow through the observer below. For an
        // already-completed task it resumes synchronously, so the field state is visible before returning.
        _ = ObserveFieldValidationTask(state, task, cts);
    }

    // Cancels and clears any validation tracked for a field, leaving it idle. Used when a new validation
    // fails to start. The prior observer disposes its own CTS and its ReferenceEquals guard prevents it
    // from stomping the cleared slot.
    private void ClearPendingFieldValidation(FieldState state)
    {
        var wasPending = state.PendingValidationTask is not null;
        var wasFaulted = state.ValidationFaulted;

        state.PendingValidationCts?.Cancel();
        state.PendingValidationTask = null;
        state.PendingValidationCts = null;
        state.ValidationFaulted = false;

        if (wasPending || wasFaulted)
        {
            NotifyValidationStateChanged();
        }
    }

    /// <summary>
    /// Returns <c>true</c> if the specified field has a pending async validation task. The task stays
    /// "pending" until the framework's observer has settled its outcome, so a consumer that waits for
    /// this to become <c>false</c> is guaranteed to also see the final
    /// <see cref="IsValidationFaulted(in FieldIdentifier)"/> value.
    /// </summary>
    /// <param name="fieldIdentifier">Identifies the field to query.</param>
    /// <returns><c>true</c> if async validation is in progress for the field; otherwise <c>false</c>.</returns>
    public bool IsValidationPending(in FieldIdentifier fieldIdentifier)
        => _fieldStates.TryGetValue(fieldIdentifier, out var state) && state.PendingValidationTask is not null;

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
    /// Returns <c>true</c> if a form-level <see cref="ValidateAsync"/> pass is currently in flight, for
    /// driving form-wide UI such as disabling a submit button. Does not consider per-field pending tasks;
    /// use the <see cref="IsValidationPending(in FieldIdentifier)"/> overload for those.
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
        => _fieldStates.TryGetValue(fieldIdentifier, out var state) && state.ValidationFaulted;

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
    /// Returns <c>true</c> if the most recent form-level pass faulted (a registered validation task threw),
    /// as opposed to merely producing validation messages. A subsequent successful pass clears it; a
    /// caller-cancelled <see cref="ValidateAsync"/> pass preserves it. For per-field faults use the
    /// <see cref="IsValidationFaulted(in FieldIdentifier)"/> overload.
    /// </summary>
    /// <returns><c>true</c> if the most recent validation pass faulted; otherwise <c>false</c>.</returns>
    public bool IsValidationFaulted() => _lastFormValidationFaulted;

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
                state.ValidationFaulted = false;
                changed = true;
            }
            else if (state.ValidationFaulted)
            {
                // A previously-faulted task already settled (CTS already cleared). Clear the lingering
                // fault so per-field IsValidationFaulted reflects only the current pass.
                state.ValidationFaulted = false;
                changed = true;
            }
        }

        if (changed)
        {
            NotifyValidationStateChanged();
        }
    }

    private async Task ObserveFieldValidationTask(FieldState state, Task task, CancellationTokenSource cts)
    {
        // Supersede any previously tracked task. Its own observer disposes its CTS when it settles, so
        // we must not dispose it here while it may still be in flight.
        state.PendingValidationCts?.Cancel();

        var completedSynchronously = task.IsCompleted;
        var hadPriorPendingTask = state.PendingValidationTask is { IsCompleted: false };
        var priorFaulted = state.ValidationFaulted;

        state.PendingValidationTask = task;
        state.PendingValidationCts = cts;
        state.ValidationFaulted = false;

        // Announce the pending transition only when we will actually suspend; a completed task never
        // observably enters the pending state.
        if (!completedSynchronously)
        {
            NotifyValidationStateChanged();
        }

        var faulted = false;
        try
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                // Cancellation initiated by us (field re-edited or form submitted) is silent.
            }
            catch
            {
                // Infrastructure fault, or an OperationCanceledException from a source other than our CTS
                // (e.g. an HttpClient timeout). Recorded as a fault but not surfaced.
                faulted = true;
            }

            // Only settle if we are still the current task: the ReferenceEquals guard makes a stale
            // (superseded) task's settle a no-op so it cannot stomp newer state.
            if (ReferenceEquals(state.PendingValidationTask, task))
            {
                state.PendingValidationTask = null;
                state.PendingValidationCts = null;
                state.ValidationFaulted = faulted;

                // A suspended task already announced pending, so settling is always a change. A completed
                // task announced nothing, so notify only if pending-ness or fault changed.
                if (!completedSynchronously || hadPriorPendingTask || faulted != priorFaulted)
                {
                    NotifyValidationStateChanged();
                }
            }
        }
        finally
        {
            // Dispose the CTS we own. Only happens after the task has settled, so the validator can
            // never observe a disposed token.
            cts.Dispose();
        }
    }
}
