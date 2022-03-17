// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Abstractions;

/// <summary>
/// A context for <see cref="IActionDescriptorProvider"/>.
/// </summary>
public class ActionDescriptorProviderContext
{
    /// <summary>
    /// Gets the <see cref="IList{T}" /> of <see cref="ActionDescriptor"/> instances of <see cref="IActionDescriptorProvider"/>
    /// can populate.
    /// </summary>
    public IList<ActionDescriptor> Results { get; } = new List<ActionDescriptor>();
}
