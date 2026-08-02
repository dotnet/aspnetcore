// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// Type that provides access to an <see cref="ActionContext"/>.
/// </summary>
[Obsolete("ActionContextAccessor is obsolete and will be removed in a future version. For more information, visit https://aka.ms/aspnet/deprecate/006.", DiagnosticId = "ASPDEPR006", UrlFormat = Obsoletions.AspNetCoreDeprecate006Url)]
public class ActionContextAccessor : IActionContextAccessor
{
    internal static readonly IActionContextAccessor Null = new NullActionContextAccessor();

    private static readonly AsyncLocal<ActionContext> _storage = new AsyncLocal<ActionContext>();

    /// <inheritdoc/>
    [DisallowNull]
    public ActionContext? ActionContext
    {
        get { return _storage.Value; }
        set { _storage.Value = value; }
    }

    private sealed class NullActionContextAccessor : IActionContextAccessor
    {
        public ActionContext? ActionContext
        {
            get => null;
            set { }
        }
    }
}
