// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.Razor.TagHelpers;

/// <summary>
/// An implementation of this interface provides the collection of <see cref="ITagHelperComponent"/>s
/// that will be used by <see cref="TagHelperComponentTagHelper"/>s.
/// </summary>
public interface ITagHelperComponentManager
{
    /// <summary>
    /// Gets the collection of <see cref="ITagHelperComponent"/>s that will be used by
    /// <see cref="TagHelperComponentTagHelper"/>s.
    /// </summary>
    ICollection<ITagHelperComponent> Components { get; }
}
