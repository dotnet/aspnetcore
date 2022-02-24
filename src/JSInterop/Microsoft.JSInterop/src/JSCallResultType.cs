// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.JSInterop;

/// <summary>
/// Describes the type of result expected from a JS interop call.
/// </summary>
public enum JSCallResultType : int
{
    /// <summary>
    /// Indicates that the returned value is not treated in a special way.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Indicates that the returned value is to be treated as a JS object reference.
    /// </summary>
    JSObjectReference = 1,

    /// <summary>
    /// Indicates that the returned value is to be treated as a JS data reference.
    /// </summary>
    JSStreamReference = 2,

    /// <summary>
    /// Indicates a void result type.
    /// </summary>
    JSVoidResult = 3,
}
