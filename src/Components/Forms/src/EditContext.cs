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
    private Exception? _formValidationException;
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
    /// Handlers run synchronously in subscription order. A validator that performs asynchronous
    /// work (for example a database lookup or a remote API call) starts that work from its handler
    /// and registers the resulting <see cref="Task"/> via
    /// <see cref="ValidationRequestedEventArgs.AddValidationTask(Task)"/>.
    /// <see cref="ValidateAsync(CancellationToken)"/> awaits every registered task before completing;
    /// <see cref="Validate"/> throws <see cref="InvalidOperationException"/> if any registered task
    /// has not already completed.
    /// <para>
    /// Do not subscribe an <c>async void</c> handler to perform asynchronous validation: its work
    /// is not awaited by <see cref="ValidateAsync"/> and is silently skipped. Instead keep the handler
    /// synchronous and register the asynchronous work with
    /// <see cref="ValidationRequestedEventArgs.AddValidationTask(Task)"/>, for example
    /// <c>(sender, args) =&gt; args.AddValidationTask(ValidateAsyncCore(args.CancellationToken))</c>.
    /// </para>
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
    /// Validates this <see cref="EditContext"/>.
    /// </summary>
    /// <returns>True if there are no validation messages after validation and no registered
    /// validation task faulted; otherwise false.</returns>
    /// <remarks>
    /// Validation must not be re-entered. Do not call <see cref="Validate"/> or
    /// <see cref="ValidateAsync"/> from inside an <see cref="OnValidationRequested"/> or
    /// <see cref="OnValidationStateChanged"/> handler attached to the same <see cref="EditContext"/>;
    /// doing so produces undefined behavior (in particular, it can cause infinite recursion via the
    /// validator's own <see cref="NotifyValidationStateChanged"/> calls).
    /// <para>
    /// A validation task that an <see cref="OnValidationRequested"/> handler registered (via
    /// <see cref="ValidationRequestedEventArgs.AddValidationTask(Task)"/>) and that completed in a
    /// faulted state is contained the same way <see cref="ValidateAsync"/> contains it: the form is
    /// marked as faulted (observable via the parameterless <see cref="IsValidationFaulted()"/> and
    /// <see cref="GetValidationException()"/>) and this method returns <c>false</c>. An exception thrown
    /// synchronously by a handler itself still propagates to the caller.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when an <see cref="OnValidationRequested"/> handler registers a validation task (via
    /// <see cref="ValidationRequestedEventArgs.AddValidationTask(Task)"/>) that has not completed
    /// synchronously. Use <see cref="ValidateAsync"/> instead when async validators are registered.
    /// </exception>
    public bool Validate()
    {
        var args = new ValidationRequestedEventArgs();
        RaiseValidationRequested(args);

        var tasks = args.ValidationTasks;
        for (var i = 0; i < tasks.Count; i++)
        {
            if (!tasks[i].IsCompleted)
            {
                // A handler registered asynchronous work that has not finished. Validate cannot
                // await it, so observe every registered task to suppress UnobservedTaskException
                // and direct the caller to ValidateAsync.
                ObserveOrphanedTasks(tasks);
                throw new InvalidOperationException(
                    $"An asynchronous validation handler did not complete synchronously. " +
                    $"Use {nameof(ValidateAsync)} instead of {nameof(Validate)} when async validators are registered.");
            }
        }

        // Every registered task has completed. Classify and contain faults the same way
        // ValidateAsync does, so the two methods surface validator faults identically via
        // IsValidationFaulted()/GetValidationException() rather than Validate rethrowing them.
        var faulted = ObserveFormValidationTasks(tasks);

        return !faulted && !GetValidationMessages().Any();
    }

    /// <summary>
    /// Validates this <see cref="EditContext"/> asynchronously.
    /// Cancels any pending field-level async validation tasks, raises the
    /// <see cref="OnValidationRequested"/> event, then awaits any validation tasks that handlers
    /// registered via <see cref="ValidationRequestedEventArgs.AddValidationTask(Task)"/>. Exceptions
    /// thrown synchronously by a handler propagate to the caller, matching <see cref="Validate"/>.
    /// Any exception other than cancellation observed on a registered task is contained: the form is
    /// marked as faulted (observable via the parameterless <see cref="IsValidationFaulted()"/>) and the
    /// method returns <c>false</c>.
    /// While registered tasks are in flight, the parameterless <see cref="IsValidationPending()"/>
    /// returns <c>true</c> so applications can show a global "validating..." indicator without wrapping
    /// the call themselves. The form-level <see cref="IsValidationFaulted()"/> result is updated only
    /// when a pass completes; it is preserved across passes cancelled by the caller.
    /// </summary>
    /// <param name="cancellationToken">A token that signals cancellation of this validation pass.
    /// The token is exposed to handlers via <see cref="ValidationRequestedEventArgs.CancellationToken"/>.
    /// If the caller cancels the token, this method throws <see cref="OperationCanceledException"/>;
    /// the form is not marked as faulted in that case and the previous form-level fault state is preserved.
    /// The token bounds the in flight pass only; field-level validation tasks that start independently
    /// during the awaited window (for example, from user edits) are not linked to this token and
    /// continue running.</param>
    /// <returns>True if there are no validation messages after validation and no registered task
    /// faulted; otherwise false.</returns>
    /// <remarks>
    /// Validation must not be re-entered. Do not call <see cref="Validate"/> or
    /// <see cref="ValidateAsync"/> from inside an <see cref="OnValidationRequested"/> or
    /// <see cref="OnValidationStateChanged"/> handler attached to the same <see cref="EditContext"/>;
    /// doing so produces undefined behavior.
    /// </remarks>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/>
    /// is cancelled before or during the validation pass.</exception>
    public async Task<bool> ValidateAsync(CancellationToken cancellationToken = default)
    {
        // Form submit validation supersedes any pending field level async tasks, so cancel them first.
        CancelAllPendingValidationTasks();

        // Mark the pass as in flight so apps can show a global "validating..." state via
        // IsValidationPending() without wrapping this call. Notify so subscribers render again.
        _isFormValidationPending = true;
        NotifyValidationStateChanged();

        var faultedThisPass = false;

        try
        {
            // Raise the event once. Synchronous validators add messages inline; validators doing
            // asynchronous work register their tasks via ValidationRequestedEventArgs.AddValidationTask.
            // The same args instance, carrying the caller's token, reaches every handler. A handler
            // that throws synchronously propagates to the caller, matching Validate().
            var args = new ValidationRequestedEventArgs(cancellationToken);
            RaiseValidationRequested(args);

            var tasks = args.ValidationTasks;
            if (tasks.Count > 0)
            {
                // Await completion; per task outcomes are inspected below, so the aggregate
                // exception WhenAll would surface adds no information.
                try
                {
                    await Task.WhenAll(tasks);
                }
                catch
                {
                    // Exception is swallowed here and classified below.
                    // Caller cancellation is rethrown via ThrowIfCancellationRequested before classification runs.
                }

                // Cancellation requested by the caller propagates to the caller. The previous form
                // level fault state is preserved since no new result was produced. The finally block
                // still clears the pending flag.
                cancellationToken.ThrowIfCancellationRequested();

                // Caller cancellation was already honored above, so any task that completed in the
                // Canceled state here was cancelled by a source other than the caller's token (for
                // example the validator's own timeout). ObserveFormValidationTasks treats both faulted
                // and such canceled tasks as faults, mirroring the per-field ObserveFieldValidationTask path.
                faultedThisPass = ObserveFormValidationTasks(tasks);
            }
            else
            {
                // No registered tasks: clear any prior fault so the form-level result reflects this pass.
                _formValidationException = null;
            }
        }
        finally
        {
            // Pending flag flips to false unconditionally, whether we completed normally, were
            // cancelled by the caller, or a handler threw. A single end of pass notification covers
            // both the pending change and any fault assignment above.
            _isFormValidationPending = false;
            NotifyValidationStateChanged();
        }

        return !faultedThisPass && !GetValidationMessages().Any();
    }

    /// <summary>
    /// Registers an asynchronous validation for a specific field. The <paramref name="validate"/>
    /// factory is invoked immediately with a <see cref="CancellationToken"/> owned by this
    /// <see cref="EditContext"/>, and the returned <see cref="Task"/> is tracked for pending/faulted
    /// state queries via <see cref="IsValidationPending(in FieldIdentifier)"/> and
    /// <see cref="IsValidationFaulted(in FieldIdentifier)"/>. If a validation is already tracked for
    /// this field, its token is cancelled and the new validation replaces it.
    /// </summary>
    /// <remarks>
    /// The <see cref="EditContext"/> creates, cancels, and disposes the
    /// <see cref="CancellationTokenSource"/> behind the supplied token; the caller never manages it.
    /// To also cancel from another source, link inside <paramref name="validate"/> via
    /// <see cref="CancellationTokenSource.CreateLinkedTokenSource(CancellationToken)"/>.
    /// <para>
    /// Write <paramref name="validate"/> as an <c>async</c> method so an exception thrown before its
    /// first <c>await</c> is captured into the returned task (and surfaced via
    /// <see cref="GetValidationException(in FieldIdentifier)"/>) rather than thrown from this method.
    /// If the returned task is already completed, it is settled synchronously: the field is never
    /// observably parked in the pending state and the token source is disposed before this method returns.
    /// If <paramref name="validate"/> throws synchronously or returns a <see langword="null"/> task, any
    /// prior validation tracked for the field is still superseded (cancelled and cleared) before the
    /// exception propagates.
    /// </para>
    /// <para>
    /// Validators are expected to clear any prior validation messages for the field up-front (before
    /// awaiting), and to avoid writing partial results to a <see cref="ValidationMessageStore"/> on a
    /// path that may subsequently throw. If a validator does write partial state and then throws, those
    /// messages remain in the store until cleared by a subsequent successful validation.
    /// </para>
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

        // EditContext owns the token source end to end: it is cancelled when a later validation
        // supersedes this one, and disposed once the task settles (both handled by the observer).
        var cts = new CancellationTokenSource();
        Task task;
        try
        {
            task = validate(cts.Token);
        }
        catch
        {
            // The new validation failed to start, so the observer will not run.
            // Clear the prior validation here so the field does not stay stuck in the
            // pending state with an outdated validator still in flight.
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

        // Supersession, parking, settle, notification, and CTS disposal all flow through the single
        // observer below. For an already-completed task the await resumes synchronously, so the
        // field's pending/faulted state and the CTS disposal are visible before this method returns.
        _ = ObserveFieldValidationTask(state, task, cts);
    }

    // Cancels and clears any validation currently tracked for a field, leaving it idle. Used when a
    // new validation fails to start (the factory threw or returned null) so that the old task is still cleared.
    // The prior validation's observer disposes its own CTS when its (now cancelled) task
    // settles, and its ReferenceEquals guard prevents it from stomping the cleared slot.
    private void ClearPendingFieldValidation(FieldState state)
    {
        var wasPending = state.PendingValidationTask is not null;
        var wasFaulted = state.ValidationException is not null;

        state.PendingValidationCts?.Cancel();
        state.PendingValidationTask = null;
        state.PendingValidationCts = null;
        state.ValidationException = null;

        if (wasPending || wasFaulted)
        {
            NotifyValidationStateChanged();
        }
    }

    /// <summary>
    /// Returns <c>true</c> if the specified field has a pending async validation task.
    /// A task is "pending" until the framework's observer has settled its outcome and cleared the
    /// slot (i.e., not only until the task itself completes) so a consumer that waits for
    /// <see cref="IsValidationPending(in FieldIdentifier)"/> to become <c>false</c> is guaranteed
    /// to also see the final <see cref="IsValidationFaulted(in FieldIdentifier)"/> value.
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
    /// (threw a non-cancellation exception). Equivalent to
    /// <c><see cref="GetValidationException(in FieldIdentifier)"/> is not null</c>.
    /// </summary>
    /// <param name="fieldIdentifier">Identifies the field to query.</param>
    /// <returns><c>true</c> if the field's last async validation faulted; otherwise <c>false</c>.</returns>
    public bool IsValidationFaulted(in FieldIdentifier fieldIdentifier)
        => GetValidationException(fieldIdentifier) is not null;

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
    /// Returns <c>true</c> if the most recent <see cref="Validate"/> or <see cref="ValidateAsync"/>
    /// pass observed an unhandled exception on a validation task that an <see cref="OnValidationRequested"/>
    /// handler registered via <see cref="ValidationRequestedEventArgs.AddValidationTask(Task)"/>. A subsequent
    /// successful pass clears the flag; a <see cref="ValidateAsync"/> pass cancelled by the caller
    /// preserves it. Use this to detect that validation itself failed (not just produced validation messages).
    /// For per-field validator faults from <see cref="TrackFieldValidation(in FieldIdentifier, Func{CancellationToken, Task})"/>,
    /// use the <see cref="IsValidationFaulted(in FieldIdentifier)"/> overload. Equivalent to
    /// <c><see cref="GetValidationException()"/> is not null</c>.
    /// </summary>
    /// <returns><c>true</c> if the most recent validation pass faulted; otherwise <c>false</c>.</returns>
    public bool IsValidationFaulted() => _formValidationException is not null;

    /// <summary>
    /// Returns the exception from the specified field's last faulted async validation, or
    /// <see langword="null"/> if the field is not faulted. This is the exception thrown by the
    /// validator (unwrapped from any single-exception <see cref="AggregateException"/>). A field
    /// whose validation was cancelled by a re-edit or a form-level pass is not faulted and returns
    /// <see langword="null"/>; a cancellation from a source unrelated to the validation's own
    /// <see cref="CancellationTokenSource"/> is treated as a fault and returns a
    /// <see cref="TaskCanceledException"/>.
    /// </summary>
    /// <param name="fieldIdentifier">Identifies the field to query.</param>
    /// <returns>The fault exception, or <see langword="null"/> if the field is not faulted.</returns>
    public Exception? GetValidationException(in FieldIdentifier fieldIdentifier)
        => _fieldStates.TryGetValue(fieldIdentifier, out var state) ? state.ValidationException : null;

    /// <summary>
    /// Returns the exception from the last faulted async validation of the field identified by the
    /// <paramref name="accessor"/> expression, or <see langword="null"/> if the field is not faulted.
    /// </summary>
    /// <typeparam name="TField">The type of the field.</typeparam>
    /// <param name="accessor">An expression that identifies the field, e.g. <c>() =&gt; model.Email</c>.</param>
    /// <returns>The fault exception, or <see langword="null"/> if the field is not faulted.</returns>
    public Exception? GetValidationException<TField>(Expression<Func<TField>> accessor)
        => GetValidationException(FieldIdentifier.Create(accessor));

    /// <summary>
    /// Returns the exception observed by the most recent <see cref="Validate"/> or
    /// <see cref="ValidateAsync"/> pass, or <see langword="null"/> if the most recent pass did not
    /// fault. When a single registered task faulted, this is that task's exception; when several
    /// faulted, the exceptions are combined into an <see cref="AggregateException"/>. A successful
    /// pass clears the value; a <see cref="ValidateAsync"/> pass cancelled by the caller preserves it.
    /// For per-field validator faults, use the <see cref="GetValidationException(in FieldIdentifier)"/> overload.
    /// </summary>
    /// <returns>The form-level fault exception, or <see langword="null"/> if the most recent pass did not fault.</returns>
    public Exception? GetValidationException() => _formValidationException;

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

    private void RaiseValidationRequested(ValidationRequestedEventArgs args)
    {
        try
        {
            OnValidationRequested?.Invoke(this, args);
        }
        catch
        {
            // A synchronous handler threw, which stops the remaining handlers in the multicast
            // chain from running. Tasks that earlier handlers already registered are now orphaned
            // (started but never awaited by this pass). Observe them so a later fault does not
            // surface as an UnobservedTaskException, then rethrow so the exception reaches the
            // caller, matching the long standing behavior of the synchronous OnValidationRequested event.
            ObserveOrphanedTasks(args.ValidationTasks);
            throw;
        }
    }

    private static void ObserveOrphanedTasks(IReadOnlyList<Task> tasks)
    {
        for (var i = 0; i < tasks.Count; i++)
        {
            // Read the Exception of any task that faults so it is observed. Tasks that complete
            // successfully or are cancelled never run this continuation.
            _ = tasks[i].ContinueWith(
                static t => _ = t.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
    }

    // Classifies the completed validation tasks of a form-level pass and stores the result in
    // _formValidationException. Shared by Validate and ValidateAsync so both surface validator faults
    // identically: a single faulted task exposes its exception, several expose an AggregateException,
    // and a pass with no faulted task clears the form-level fault. Returns whether the pass faulted.
    private bool ObserveFormValidationTasks(IReadOnlyList<Task> tasks)
    {
        List<Exception>? faults = null;
        for (var i = 0; i < tasks.Count; i++)
        {
            var task = tasks[i];
            if (task.IsFaulted)
            {
                (faults ??= new List<Exception>()).AddRange(task.Exception!.InnerExceptions);
            }
            else if (task.IsCanceled)
            {
                // Callers reach this only after caller cancellation has been honored, so a Canceled
                // task was cancelled by a source other than the caller (e.g. the validator's own
                // timeout) and is treated as an infrastructure fault, mirroring the per-field
                // ObserveFieldValidationTask path. A Canceled task carries no Exception object, so
                // synthesize a TaskCanceledException so the fault is retrievable via GetValidationException().
                (faults ??= new List<Exception>()).Add(new TaskCanceledException(task));
            }
        }

        // Assign the new fault result in one step so consumers querying IsValidationFaulted() during
        // the pass continue to see the previous result instead of a brief reset then set flicker.
        _formValidationException = faults is null
            ? null
            : faults.Count == 1
                ? faults[0]
                : new AggregateException(faults);

        return faults is not null;
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
                state.ValidationException = null;
                changed = true;
            }
            else if (state.ValidationException is not null)
            {
                // Field had a previously-faulted task that already settled (so its CTS was
                // already cleared). Clear the lingering fault at the start of the new validation
                // pass so per-field IsValidationFaulted reflects only the current pass's outcome.
                state.ValidationException = null;
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

    private async Task ObserveFieldValidationTask(FieldState state, Task task, CancellationTokenSource cts)
    {
        // Supersede any previously tracked task for this field. Its own observer disposes its CTS
        // when it settles, so we must not dispose it here while it may still be in flight.
        state.PendingValidationCts?.Cancel();

        var completedSynchronously = task.IsCompleted;
        var hadPriorPendingTask = state.PendingValidationTask is { IsCompleted: false };
        var priorFault = state.ValidationException;

        state.PendingValidationTask = task;
        state.PendingValidationCts = cts;
        state.ValidationException = null;

        // Announce the pending transition only when we will actually suspend. A completed task never
        // observably enters the pending state, so its only notification (if any) is at settle below.
        if (!completedSynchronously)
        {
            NotifyValidationStateChanged();
        }

        Exception? fault = null;
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
            catch (Exception ex)
            {
                // Infrastructure fault, or an OperationCanceledException from a source other than our
                // CTS (e.g. an HttpClient timeout inside the validator). A faulted task exposes its
                // exceptions on task.Exception (unwrap the single-exception case); a Canceled task has
                // none, so fall back to the caught exception.
                fault = task.Exception is { } aggregate ? Unwrap(aggregate) : ex;
            }

            // Only settle if we are still the current task for this field. A superseding registration
            // replaces the slot, and this ReferenceEquals check makes a stale task's settle a no-op so
            // it cannot stomp newer state.
            if (ReferenceEquals(state.PendingValidationTask, task))
            {
                state.PendingValidationTask = null;
                state.PendingValidationCts = null;
                state.ValidationException = fault;

                // A task that suspended already announced pending, so leaving it is always a change.
                // A completed task announced nothing, so notify only if the field's pending-ness or
                // fault actually changed.
                if (!completedSynchronously || hadPriorPendingTask || (fault is null) != (priorFault is null))
                {
                    NotifyValidationStateChanged();
                }
            }
        }
        finally
        {
            // Always dispose the CTS we own. Safe whether or not the slot is still current,
            // and only happens after the validator task has observably settled, so the validator
            // can never observe a disposed token.
            cts.Dispose();
        }
    }

    private static Exception Unwrap(AggregateException aggregate)
        => aggregate.InnerExceptions.Count == 1 ? aggregate.InnerExceptions[0] : aggregate;
}
