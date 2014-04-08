// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Security.Notifications
{
    public abstract class EndpointContext : BaseContext
    {
        protected EndpointContext(HttpContext context)
            : base(context)
        {
        }

        public bool IsRequestCompleted { get; private set; }

        public void RequestCompleted()
        {
            IsRequestCompleted = true;
        }
    }
}
