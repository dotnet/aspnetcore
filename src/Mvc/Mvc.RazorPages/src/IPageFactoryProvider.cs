// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Rendering;

namespace Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// Provides methods for creation and disposal of Razor pages.
/// </summary>
public interface IPageFactoryProvider
{
    /// <summary>
    /// Creates a factory for producing Razor pages for the specified <see cref="PageContext"/>.
    /// </summary>
    /// <param name="descriptor">The <see cref="CompiledPageActionDescriptor"/>.</param>
    /// <returns>The Razor page factory.</returns>
    Func<PageContext, ViewContext, object> CreatePageFactory(CompiledPageActionDescriptor descriptor);

    /// <summary>
    /// Releases a Razor page.
    /// </summary>
    /// <param name="descriptor">The <see cref="CompiledPageActionDescriptor"/>.</param>
    /// <returns>The delegate used to release the created page.</returns>
    Action<PageContext, ViewContext, object>? CreatePageDisposer(CompiledPageActionDescriptor descriptor);

    /// <summary>
    /// Releases a Razor page asynchronously.
    /// </summary>
    /// <param name="descriptor">The <see cref="CompiledPageActionDescriptor"/>.</param>
    /// <returns>The delegate used to release the created page asynchronously.</returns>
    Func<PageContext, ViewContext, object, ValueTask>? CreateAsyncPageDisposer(CompiledPageActionDescriptor descriptor)
    {
        var disposer = CreatePageDisposer(descriptor);
        if (disposer is null)
        {
            return null;
        }

        return (context, viewContext, page) =>
        {
            disposer(context, viewContext, page);
            return default;
        };
    }
}
