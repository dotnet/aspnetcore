// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Used to normalize keys for consistent lookups
    /// </summary>
    public interface ILookupNormalizer
    {
        /// <summary>
        /// Returns the normalized key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        string Normalize(string key);
    }
}