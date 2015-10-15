// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Authentication.Twitter
{
    /// <summary>
    /// Base class for other Twitter contexts.
    /// </summary>
    public class BaseTwitterContext : BaseContext
    {
        /// <summary>
        /// Initializes a <see cref="BaseTwitterContext"/>
        /// </summary>
        /// <param name="context">The HTTP environment</param>
        /// <param name="options">The options for Twitter</param>
        public BaseTwitterContext(HttpContext context, TwitterOptions options)
            : base(context)
        {
            Options = options;
        }

        public TwitterOptions Options { get; }
    }
}
