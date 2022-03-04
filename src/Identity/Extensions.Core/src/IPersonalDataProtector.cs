// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Identity
{
    /// <summary>
    /// Provides an abstraction used for personal data encryption.
    /// </summary>
    public interface IPersonalDataProtector
    {
        /// <summary>
        /// Protect the data.
        /// </summary>
        /// <param name="data">The data to protect.</param>
        /// <returns>The protected data.</returns>
        string Protect(string data);

        /// <summary>
        /// Unprotect the data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>The unprotected data.</returns>
        string Unprotect(string data);
    }
}