// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.JSInterop.Infrastructure;

/// <summary>
/// Describes type of operation invoked in JavaScript via interop.
/// </summary>
public enum JSCallType : int
{
    /// <summary>
    /// Represents a regular function invocation.
    /// </summary>
    FunctionCall = 1,

    /// <summary>
    /// Represents a constructor function invocation with the <c>new</c> operator.
    /// </summary>
    NewCall = 2,

    /// <summary>
    /// Represents reading a property value.
    /// </summary>
    GetValue = 3,

    /// <summary>
    /// Represents updating or defining a property value.
    /// </summary>
    SetValue = 4,
}
