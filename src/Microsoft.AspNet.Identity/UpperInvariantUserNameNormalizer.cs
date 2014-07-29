// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    /// Normalizes user names via ToUpperInvariant()
    /// </summary>
    public class UpperInvariantUserNameNormalizer : IUserNameNormalizer
    {
        /// <summary>
        /// Normalizes user names via ToUpperInvariant()
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public string Normalize(string userName)
        {
            if (userName == null)
            {
                return null;
            }
            return userName.Normalize().ToUpperInvariant();
        }
    }
}