// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.Twitter
{
    /// <summary>
    /// Base class for other Twitter contexts.
    /// </summary>
    public class BaseTwitterContext : BaseAuthenticationContext
    {
        /// <summary>
        /// Initializes a <see cref="BaseTwitterContext"/>
        /// </summary>
        /// <param name="context">The HTTP environment</param>
        /// <param name="scheme">The scheme data</param>
        /// <param name="options">The options for Twitter</param>
        /// <param name="properties">The AuthenticationProperties</param>
        public BaseTwitterContext(HttpContext context, AuthenticationScheme scheme, TwitterOptions options, AuthenticationProperties properties)
            : base(context, scheme.Name, properties)
        {
            Options = options;
        }

        public TwitterOptions Options { get; }

        public AuthenticationScheme Scheme { get; }
    }
}
