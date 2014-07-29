// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Used to normalize a user name
    /// </summary>
    public interface IUserNameNormalizer
    {
        /// <summary>
        /// Returns the normalized user name
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        string Normalize(string userName);
    }
}