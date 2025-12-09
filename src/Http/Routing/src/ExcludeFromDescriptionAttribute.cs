// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Indicates that this <see cref="Endpoint"/> should not be included in the generated API metadata.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Delegate, AllowMultiple = false, Inherited = true)]
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ExcludeFromDescriptionAttribute : Attribute, IExcludeFromDescriptionMetadata
{
    /// <inheritdoc />
    public bool ExcludeFromDescription => true;

    /// <inheritdoc/>
    public override string ToString()
    {
        return DebuggerHelpers.GetDebugText(nameof(ExcludeFromDescription), ExcludeFromDescription);
    }
}
