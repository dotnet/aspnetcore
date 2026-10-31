// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ObjectPool;

/// <summary>
/// Defines a method to reset an object to its initial state.
/// </summary>
public interface IResettable
{
    /// <summary>
    /// Reset the object to a neutral state, semantically similar to when the object was first constructed.
    /// </summary>
    /// <returns><see langword="true" /> if the object was able to reset itself, otherwise <see langword="false" />.</returns>
    /// <remarks>
    /// In general, this method is not expected to be thread-safe.
    /// </remarks>
    bool TryReset();
}
