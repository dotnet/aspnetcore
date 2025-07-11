// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// Defines an interface for exposing an <see cref="ActionContext"/>.
/// </summary>
[Obsolete("IActionContextAccessor is obsolete. Use IHttpContextAccessor instead and access the endpoint information from HttpContext.GetEndpoint(). This type will be removed in a future version.")]
public interface IActionContextAccessor
{
    /// <summary>
    /// Gets or sets the <see cref="ActionContext"/>.
    /// </summary>
    [DisallowNull]
    ActionContext? ActionContext { get; set; }
}
