// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public class IISServerOptions
    {
        /// <summary>
        /// If true the server should set HttpContext.User. If false the server will only provide an
        /// identity when explicitly requested by the AuthenticationScheme.
        /// Note Windows Authentication must also be enabled in IIS for this to work.
        /// </summary>
        public bool AutomaticAuthentication { get; set; } = true;

        /// <summary>
        /// Sets the display name shown to users on login pages. The default is null.
        /// </summary>
        public string AuthenticationDisplayName { get; set; }

        /// <summary>
        /// Used to indicate if the authentication handler should be registered. This is only done if ANCM indicates
        /// IIS has a non-anonymous authentication enabled, or for back compat with ANCMs that did not provide this information.
        /// </summary>
        internal bool ForwardWindowsAuthentication { get; set; } = true;

        internal string[] ServerAddresses { get; set; }
    }
}
