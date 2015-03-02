// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http.Authentication;

namespace Microsoft.AspNet.Authentication
{
    /// <summary>
    /// Base Options for all automatic authentication middleware
    /// </summary>
    public abstract class AutomaticAuthenticationOptions : AuthenticationOptions
    {
        /// <summary>
        /// If true the authentication middleware alter the request user coming in and
        /// alter 401 Unauthorized responses going out. If false the authentication middleware will only provide
        /// identity and alter responses when explicitly indicated by the AuthenticationScheme.
        /// </summary>
        public bool AutomaticAuthentication { get; set; }
    }
}
