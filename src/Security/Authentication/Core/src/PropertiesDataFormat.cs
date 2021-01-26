// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.DataProtection;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// A <see cref="SecureDataFormat{TData}"/> instance to secure
    /// <see cref="AuthenticationProperties"/>.
    /// </summary>
    public class PropertiesDataFormat : SecureDataFormat<AuthenticationProperties>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PropertiesDataFormat"/>.
        /// </summary>
        /// <param name="protector">The <see cref="IDataProtector"/>.</param>
        public PropertiesDataFormat(IDataProtector protector)
            : base(new PropertiesSerializer(), protector)
        {
        }
    }
}
