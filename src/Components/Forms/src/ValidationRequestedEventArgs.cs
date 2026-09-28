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
    /// completes. The method must not return a <see langword="null"/> task.</param>
    /// <exception cref="ArgumentNullException"><paramref name="validator"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="validator"/> returned <see langword="null"/>, or this is the shared non-async <see cref="Empty"/> instance.
    /// </exception>
    public void AddAsyncValidator(Func<CancellationToken, Task> validator)
    {
        ArgumentNullException.ThrowIfNull(validator);

        if (!IsAsync)
        {
            throw new InvalidOperationException(
                $"Asynchronous validation is not supported during a synchronous {nameof(EditContext)}.{nameof(EditContext.Validate)} call. " +
                $"Call {nameof(EditContext.ValidateAsync)} instead.");
        }

        (_asyncValidators ??= []).Add(validator);
    }

    internal IReadOnlyList<Func<CancellationToken, Task>> AsyncValidators
        => _asyncValidators ?? (IReadOnlyList<Func<CancellationToken, Task>>)[];
}
