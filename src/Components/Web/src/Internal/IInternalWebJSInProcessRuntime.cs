// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web.Internal;

/// <summary>
/// For internal framework use only.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IInternalWebJSInProcessRuntime
{
    /// <summary>
    /// For internal framework use only.
    /// </summary>
    string InvokeJS(string identifier, [StringSyntax(StringSyntaxAttribute.Json)] string? argsJson, JSCallResultType resultType, long targetInstanceId);
}
