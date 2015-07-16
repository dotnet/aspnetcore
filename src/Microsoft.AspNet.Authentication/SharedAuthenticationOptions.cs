// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;

namespace Microsoft.AspNet.Authentication
{
    public class SharedAuthenticationOptions
    {
        /// <summary>
        /// Gets or sets the authentication scheme corresponding to the default middleware
        /// responsible of persisting user's identity after a successful authentication.
        /// This value typically corresponds to a cookie middleware registered in the Startup class.
        /// </summary>
        public string SignInScheme { get; set; }
    }
}
