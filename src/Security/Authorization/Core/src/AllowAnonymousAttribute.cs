// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Authorization;

/// <summary>
/// Specifies that the class or method that this attribute is applied to does not require authorization.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
[DebuggerDisplay("{ToString(),nq}")]
public class AllowAnonymousAttribute : Attribute, IAllowAnonymous
{
    /// <inheritdoc/>
    public override string ToString()
    {
        return "AllowAnonymous";
    }
}
