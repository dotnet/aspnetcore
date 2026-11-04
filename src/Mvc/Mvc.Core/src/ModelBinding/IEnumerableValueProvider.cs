// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// Interface representing an enumerable <see cref="IValueProvider"/>.
/// </summary>
public interface IEnumerableValueProvider : IValueProvider
{
    /// <summary>
    /// Gets the keys for a specific prefix.
    /// </summary>
    /// <param name="prefix">The prefix to enumerate.</param>
    /// <returns>The keys for the prefix.</returns>
    IDictionary<string, string> GetKeysFromPrefix(string prefix);
}
