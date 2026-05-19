// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

/// <summary>
/// Creates a <see cref="CompiledPageActionDescriptor"/> from a <see cref="PageActionDescriptor"/>.
/// </summary>
[Obsolete("This type is obsolete. Use " + nameof(PageLoader) + " instead.")]
public interface IPageLoader
{
    /// <summary>
    /// Produces a <see cref="CompiledPageActionDescriptor"/> given a <see cref="PageActionDescriptor"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="PageActionDescriptor"/>.</param>
    /// <returns>The <see cref="CompiledPageActionDescriptor"/>.</returns>
    CompiledPageActionDescriptor Load(PageActionDescriptor actionDescriptor);
}
