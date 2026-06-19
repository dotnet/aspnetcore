// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Provides information about the <see cref="EditContext.OnValidationRequested"/> event.
/// </summary>
public sealed class ValidationRequestedEventArgs : EventArgs
{
    private readonly bool _isReadOnly;
    private List<Task>? _validationTasks;

    /// <summary>
    /// Gets a shared empty instance of <see cref="ValidationRequestedEventArgs"/>.
    /// </summary>
    public static new readonly ValidationRequestedEventArgs Empty = new ValidationRequestedEventArgs(isReadOnly: true);

    /// <summary>
    /// Creates a new instance of <see cref="ValidationRequestedEventArgs"/>.
    /// </summary>
    public ValidationRequestedEventArgs()
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="ValidationRequestedEventArgs"/> with the specified
    /// <see cref="System.Threading.CancellationToken"/>.
    /// </summary>
    /// <param name="cancellationToken">A token that signals when the caller has requested
    /// cancellation of the in-flight async validation pass.</param>
    public ValidationRequestedEventArgs(CancellationToken cancellationToken)
    {
        CancellationToken = cancellationToken;
    }

    private ValidationRequestedEventArgs(bool isReadOnly)
    {
        _isReadOnly = isReadOnly;
    }

    /// <summary>
    /// Gets a token that signals when the caller has requested cancellation of the in-flight
    /// async validation pass. Synchronous handlers can ignore this; async handlers that perform
    /// long-running work (database lookups, remote API calls) should pass it to their downstream
    /// APIs so the work can be aborted promptly.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Registers an asynchronous validation task to be awaited as part of the current validation pass.
    /// </summary>
    /// <param name="task">The asynchronous validation task to track for the current validation pass.</param>
    /// <remarks>
    /// A validator that needs to perform asynchronous work subscribes to
    /// <see cref="EditContext.OnValidationRequested"/>, starts that work from its handler, and passes
    /// the resulting <see cref="Task"/> to this method. <see cref="EditContext.ValidateAsync(CancellationToken)"/>
    /// awaits every registered task before completing, while <see cref="EditContext.Validate"/> throws
    /// <see cref="InvalidOperationException"/> if a registered task has not already completed.
    /// <para>
    /// Start the asynchronous work with an <c>async</c> method so that any exception thrown before its
    /// first <c>await</c> is captured into the returned task rather than thrown from the handler. Do not
    /// subscribe an <c>async void</c> handler instead of calling this method: such a handler is not
    /// awaited by <see cref="EditContext.ValidateAsync(CancellationToken)"/> and its validation is
    /// silently skipped.
    /// </para>
    /// <example>
    /// <code>
    /// editContext.OnValidationRequested += (sender, args) =&gt;
    /// {
    ///     args.AddValidationTask(ValidateModelAsync(editContext.Model, args.CancellationToken));
    /// };
    /// </code>
    /// </example>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="task"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// This instance is the shared read only <see cref="Empty"/> instance, which does not collect tasks.
    /// </exception>
    public void AddValidationTask(Task task)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (_isReadOnly)
        {
            throw new InvalidOperationException(
                $"Cannot register a validation task on the shared {nameof(ValidationRequestedEventArgs)}.{nameof(Empty)} instance. " +
                $"Register tasks on the instance supplied to your {nameof(EditContext.OnValidationRequested)} handler instead.");
        }

        (_validationTasks ??= []).Add(task);
    }

    internal IReadOnlyList<Task> ValidationTasks
        => _validationTasks is { } tasks ? tasks : Array.Empty<Task>();
}
