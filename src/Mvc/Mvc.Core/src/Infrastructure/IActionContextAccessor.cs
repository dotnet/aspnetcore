// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// Defines an interface for exposing an <see cref="ActionContext"/>.
/// </summary>
[Obsolete("IActionContextAccessor is obsolete and will be removed in a future version. For more information, visit https://aka.ms/aspnet/deprecate/006.", DiagnosticId = "ASPDEPR006")]
public interface IActionContextAccessor
{
    /// <summary>
    /// Gets or sets the <see cref="ActionContext"/>.
    /// </summary>
    [DisallowNull]
    ActionContext? ActionContext { get; set; }
}
