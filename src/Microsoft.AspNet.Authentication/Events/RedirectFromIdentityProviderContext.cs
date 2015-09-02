// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Authentication
{
    public class RedirectFromIdentityProviderContext<TMessage, TOptions> : BaseControlContext<TOptions>
    {
        public RedirectFromIdentityProviderContext(HttpContext context, TOptions options)
            : base(context, options)
        {
        }

        public string SignInScheme { get; set; }

        public bool IsRequestCompleted { get; set; }

        public TMessage ProtocolMessage { get; set; }
    }
}
