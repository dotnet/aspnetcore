// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
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
}
