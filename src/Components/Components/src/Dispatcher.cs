// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Sections;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Dispatches external actions to be executed on the context of a <see cref="Renderer"/>.
/// </summary>
public abstract class Dispatcher
{
    private SectionRegistry? _sectionRegistry;

    /// <summary>
    /// Creates a default instance of <see cref="Dispatcher"/>.
    /// </summary>
    /// <returns>A <see cref="Dispatcher"/> instance.</returns>
    public static Dispatcher CreateDefault() => new RendererSynchronizationContextDispatcher();

    /// <summary>
    /// Provides notifications of unhandled exceptions that occur within the dispatcher.
    /// </summary>
    internal event UnhandledExceptionEventHandler? UnhandledException;

    /// <summary>
    /// Gets the <see cref="Sections.SectionRegistry"/> associated with the dispatcher.
    /// </summary>
    internal SectionRegistry SectionRegistry => _sectionRegistry ??= new();

    /// <summary>
    /// Validates that the currently executing code is running inside the dispatcher.
    /// </summary>
    public void AssertAccess()
    {
        if (!CheckAccess())
        {
            throw new InvalidOperationException(
                "The current thread is not associated with the Dispatcher. " +
                "Use InvokeAsync() to switch execution to the Dispatcher when " +
                "triggering rendering or component state.");
        }
    }

    /// <summary>
    /// Returns a value that determines whether using the dispatcher to invoke a work item is required
    /// from the current context.
    /// </summary>
    /// <returns><c>true</c> if invoking is required, otherwise <c>false</c>.</returns>
    public abstract bool CheckAccess();

    /// <summary>
    /// Invokes the given <see cref="Action"/> in the context of the associated <see cref="Renderer"/>.
    /// </summary>
    /// <param name="workItem">The action to execute.</param>
    /// <returns>A <see cref="Task"/> that will be completed when the action has finished executing.</returns>
    public abstract Task InvokeAsync(Action workItem);

    /// <summary>
    /// Invokes the given <see cref="Func{TResult}"/> in the context of the associated <see cref="Renderer"/>.
    /// </summary>
    /// <param name="workItem">The asynchronous action to execute.</param>
    /// <returns>A <see cref="Task"/> that will be completed when the action has finished executing.</returns>
    public abstract Task InvokeAsync(Func<Task> workItem);

    /// <summary>
    /// Invokes the given <see cref="Func{TResult}"/> in the context of the associated <see cref="Renderer"/>.
    /// </summary>
    /// <param name="workItem">The function to execute.</param>
    /// <returns>A <see cref="Task{TResult}"/> that will be completed when the function has finished executing.</returns>
    public abstract Task<TResult> InvokeAsync<TResult>(Func<TResult> workItem);

    /// <summary>
    /// Invokes the given <see cref="Func{TResult}"/> in the context of the associated <see cref="Renderer"/>.
    /// </summary>
    /// <param name="workItem">The asynchronous function to execute.</param>
    /// <returns>A <see cref="Task{TResult}"/> that will be completed when the function has finished executing.</returns>
    public abstract Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> workItem);

    /// <summary>
    /// Called to notify listeners of an unhandled exception.
    /// </summary>
    /// <param name="e">The <see cref="UnhandledExceptionEventArgs"/>.</param>
    protected void OnUnhandledException(UnhandledExceptionEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        UnhandledException?.Invoke(this, e);
    }
}
