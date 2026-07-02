// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Provides information about the <see cref="EditContext.OnValidationRequested"/> event.
/// </summary>
public sealed class ValidationRequestedEventArgs : EventArgs
{
    private List<Func<CancellationToken, Task>>? _asyncValidators;

    /// <summary>
    /// Gets a shared empty instance of <see cref="ValidationRequestedEventArgs"/>.
    /// </summary>
    public static new readonly ValidationRequestedEventArgs Empty = new ValidationRequestedEventArgs();

    /// <summary>
    /// Creates a new instance of <see cref="ValidationRequestedEventArgs"/>.
    /// </summary>
    public ValidationRequestedEventArgs()
    {
    }

    /// <summary>
    /// Gets a value indicating whether the current validation pass awaits asynchronous work.
    /// Async validation can be registered via <see cref="AddAsyncValidator(Func{CancellationToken, Task})"/> only
    /// when this is <see langword="true"/>.
    /// </summary>
    internal bool IsAsync { get; init; }

    /// <summary>
    /// Registers an asynchronous validation to be run and awaited as part of the current validation pass.
    /// </summary>
    /// <param name="validator">A validator method that starts the asynchronous validation work and returns the
    /// resulting <see cref="Task"/>. It is invoked by <see cref="EditContext.ValidateAsync(CancellationToken)"/>
    /// with the validation pass's cancellation token, and the returned task is awaited before the pass
    /// completes. The method must not return a <see langword="null"/> task; doing so throws
    /// <see cref="InvalidOperationException"/> from <see cref="EditContext.ValidateAsync(CancellationToken)"/>.</param>
    /// <remarks>
    /// Subscribe to <see cref="EditContext.OnValidationRequested"/>, check that <see cref="IsAsync"/> is
    /// <see langword="true"/>, and register a validator with this method. Registered validators are invoked
    /// together so the tasks run concurrently. A validator that throws synchronously, or returns a
    /// <see langword="null"/> task, is a programming error that propagates out of
    /// <see cref="EditContext.ValidateAsync(CancellationToken)"/>. Calling this method when <see cref="IsAsync"/> is <see langword="false"/>
    /// (a synchronous <see cref="EditContext.Validate"/> pass, or the shared <see cref="Empty"/> instance)
    /// throws <see cref="InvalidOperationException"/> without invoking the validator.
    /// <example>
    /// <code>
    /// editContext.OnValidationRequested += (sender, args) =&gt;
    /// {
    ///     if (args.IsAsync)
    ///     {
    ///         args.AddAsyncValidator(token =&gt; ValidateModelAsync(editContext.Model, token));
    ///     }
    /// };
    /// </code>
    /// </example>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="validator"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// <see cref="IsAsync"/> is <see langword="false"/>: validation was started by the synchronous
    /// <see cref="EditContext.Validate"/>, or this is the shared non-async <see cref="Empty"/> instance.
    /// </exception>
    public void AddAsyncValidator(Func<CancellationToken, Task> validator)
    {
        ArgumentNullException.ThrowIfNull(validator);

        if (!IsAsync)
        {
            // The validator is not invoked, so no async work starts. Async validation is not permitted in a
            // synchronous Validate() pass, and the shared non-async Empty instance must not be mutated.
            throw new InvalidOperationException(
                $"Asynchronous validation is not supported during a synchronous {nameof(EditContext)}.{nameof(EditContext.Validate)} call. " +
                $"Call {nameof(EditContext.ValidateAsync)} instead, or guard the handler with {nameof(ValidationRequestedEventArgs)}.{nameof(IsAsync)}.");
        }

        (_asyncValidators ??= []).Add(validator);
    }

    internal IReadOnlyList<Func<CancellationToken, Task>> AsyncValidators
        => _asyncValidators ?? (IReadOnlyList<Func<CancellationToken, Task>>)[];
}
