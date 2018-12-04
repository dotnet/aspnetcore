// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Localization
{
    /// <summary>
    /// Represents a cache of string names in resources.
    /// </summary>
    public interface IResourceNamesCache
    {
        /// <summary>
        /// Adds a set of resource names to the cache by using the specified function, if the name does not already exist.
        /// </summary>
        /// <param name="name">The resource name to add string names for.</param>
        /// <param name="valueFactory">The function used to generate the string names for the resource.</param>
        /// <returns>The string names for the resource.</returns>
        IList<string> GetOrAdd(string name, Func<string, IList<string>> valueFactory);
    }
}
