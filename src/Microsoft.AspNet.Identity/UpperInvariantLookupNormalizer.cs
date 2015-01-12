// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    /// Normalizes via ToUpperInvariant()
    /// </summary>
    public class UpperInvariantLookupNormalizer : ILookupNormalizer
    {
        /// <summary>
        /// Normalizes via ToUpperInvariant()
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string Normalize(string key)
        {
            if (key == null)
            {
                return null;
            }
            return key.Normalize().ToUpperInvariant();
        }
    }
}