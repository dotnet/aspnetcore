// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// Provides methods to create a Razor Page model.
/// </summary>
public interface IPageModelActivatorProvider
{
    /// <summary>
    /// Creates a Razor Page model activator.
    /// </summary>
    /// <param name="descriptor">The <see cref="CompiledPageActionDescriptor"/>.</param>
    /// <returns>The delegate used to activate the page model.</returns>
    Func<PageContext, object> CreateActivator(CompiledPageActionDescriptor descriptor);

    /// <summary>
    /// Releases a Razor Page model.
    /// </summary>
    /// <param name="descriptor">The <see cref="CompiledPageActionDescriptor"/>.</param>
    /// <returns>The delegate used to dispose the activated Razor Page model.</returns>
    Action<PageContext, object>? CreateReleaser(CompiledPageActionDescriptor descriptor);

    /// <summary>
    /// Releases a Razor Page model asynchronously.
    /// </summary>
    /// <param name="descriptor">The <see cref="CompiledPageActionDescriptor"/>.</param>
    /// <returns>The delegate used to dispose the activated Razor Page model asynchronously.</returns>
    Func<PageContext, object, ValueTask>? CreateAsyncReleaser(CompiledPageActionDescriptor descriptor)
    {
        var releaser = CreateReleaser(descriptor);
        if (releaser is null)
        {
            return null;
        }

        return (context, model) =>
        {
            releaser(context, model);
            return default;
        };
    }
}
