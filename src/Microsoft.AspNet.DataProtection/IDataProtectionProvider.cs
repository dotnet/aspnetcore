// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.DataProtection
{
    /// <summary>
    /// An interface that can be used to create IDataProtector instances.
    /// </summary>
    public interface IDataProtectionProvider
    {
        /// <summary>
        /// Creates an IDataProtector given a purpose.
        /// </summary>
        /// <param name="purposes">
        /// The purpose to be assigned to the newly-created IDataProtector.
        /// This parameter must be unique for the intended use case; two different IDataProtector
        /// instances created with two different 'purpose' strings will not be able
        /// to understand each other's payloads. The 'purpose' parameter is not intended to be
        /// kept secret.
        /// </param>
        /// <returns>An IDataProtector tied to the provided purpose.</returns>
        IDataProtector CreateProtector(string purpose);
    }
}
