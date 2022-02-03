// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

/// <summary>
/// Represents a description of a handler method.
/// </summary>
public class HandlerMethodDescriptor
{
    /// <summary>
    /// Gets or sets the <see cref="MethodInfo"/>.
    /// </summary>
    public MethodInfo MethodInfo { get; set; } = default!;

    /// <summary>
    /// Gets or sets the http method.
    /// </summary>
    public string HttpMethod { get; set; } = default!;

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the parameters for the method.
    /// </summary>
    public IList<HandlerParameterDescriptor> Parameters { get; set; } = default!;
}
