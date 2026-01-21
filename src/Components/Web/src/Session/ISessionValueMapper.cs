// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Maps session data values to a model.
/// </summary>
public interface ISessionValueMapper
{
    /// <summary>
    /// Returns the session value with the specified name.
    /// </summary>
    object? GetValue(string sessionKey);
}
