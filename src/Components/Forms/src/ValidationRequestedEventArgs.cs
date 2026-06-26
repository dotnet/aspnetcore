// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Provides information about the <see cref="EditContext.OnValidationRequested"/> event.
/// </summary>
public sealed class ValidationRequestedEventArgs : EventArgs
{
    private List<Func<CancellationToken, Task>>? _validationTaskFactories;

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
    /// This is <see langword="true"/> when validation was started by
    /// <see cref="EditContext.ValidateAsync(CancellationToken)"/> and <see langword="false"/> when it
    /// was started by the synchronous <see cref="EditContext.Validate"/>. A handler that performs
    /// asynchronous validation registers its work via <see cref="AddValidationTask(Func{CancellationToken, Task})"/> only when
    /// this is <see langword="true"/>; calling <see cref="AddValidationTask(Func{CancellationToken, Task})"/> while this is
    /// <see langword="false"/> throws <see cref="InvalidOperationException"/>.
    /// </summary>
    public bool IsAsync { get; internal init; }

    /// <summary>
    /// Registers an asynchronous validation to be run and awaited as part of the current validation pass.
    /// </summary>
    /// <param name="validate">A factory that starts the asynchronous validation work and returns the
    /// resulting <see cref="Task"/>. The factory is invoked by
    /// <see cref="EditContext.ValidateAsync(CancellationToken)"/> with a cancellation token, and the
    /// returned task is awaited before the pass completes. The factory must not return a
    /// <see langword="null"/> task; doing so throws <see cref="ArgumentNullException"/> from
    /// <see cref="EditContext.ValidateAsync(CancellationToken)"/>.</param>
    /// <remarks>
    /// A validator that needs to perform asynchronous work subscribes to
    /// <see cref="EditContext.OnValidationRequested"/>, checks that <see cref="IsAsync"/> is
    /// <see langword="true"/>, and registers a factory with this method.
    /// <see cref="EditContext.ValidateAsync(CancellationToken)"/> invokes every registered factory and
    /// awaits the resulting tasks before completing. Factories are invoked together so the validators
    /// run concurrently; the token passed to the factory is the one supplied to
    /// <see cref="EditContext.ValidateAsync(CancellationToken)"/>. An exception thrown synchronously by the factory (or a task that
    /// faults) is contained as a validation fault rather than propagating out of <see cref="EditContext.ValidateAsync(CancellationToken)"/>.
    /// <para>
    /// Asynchronous validation is not supported by the synchronous <see cref="EditContext.Validate"/>.
    /// Calling this method during a <see cref="EditContext.Validate"/> pass (when <see cref="IsAsync"/>
    /// is <see langword="false"/>) throws <see cref="InvalidOperationException"/> without invoking the
    /// factory, so an asynchronous validator can never be silently skipped and its work never starts.
    /// Branch on <see cref="IsAsync"/> to run synchronous validation in that case.
    /// </para>
    /// <example>
    /// <code>
    /// editContext.OnValidationRequested += (sender, args) =&gt;
    /// {
    ///     if (args.IsAsync)
    ///     {
    ///         args.AddValidationTask(token =&gt; ValidateModelAsync(editContext.Model, token));
    ///     }
    /// };
    /// </code>
    /// </example>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="validate"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// <see cref="IsAsync"/> is <see langword="false"/>: validation was started by the synchronous
    /// <see cref="EditContext.Validate"/>, or this is the shared non-async <see cref="Empty"/> instance.
    /// </exception>
    public void AddValidationTask(Func<CancellationToken, Task> validate)
    {
        ArgumentNullException.ThrowIfNull(validate);

        if (!IsAsync)
        {
            // The factory is not invoked, so no asynchronous work starts. Asynchronous validation is
            // not permitted during a synchronous Validate() pass, and the shared non-async Empty
            // instance must not be mutated.
            throw new InvalidOperationException(
                $"Asynchronous validation is not supported during a synchronous {nameof(EditContext)}.{nameof(EditContext.Validate)} call. " +
                $"Call {nameof(EditContext.ValidateAsync)} instead, or guard the handler with {nameof(ValidationRequestedEventArgs)}.{nameof(IsAsync)}.");
        }

        (_validationTaskFactories ??= []).Add(validate);
    }

    internal IReadOnlyList<Func<CancellationToken, Task>> ValidationTaskFactories
        => _validationTaskFactories ?? (IReadOnlyList<Func<CancellationToken, Task>>)[];
}
