// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Security.Notifications
{
    public class SecurityTokenReceivedNotification<TMessage, TOptions> : BaseNotification<TOptions>
    {
        public SecurityTokenReceivedNotification(HttpContext context, TOptions options) : base(context, options)
        {
        }

        public string SecurityToken { get; set; }

        public TMessage ProtocolMessage { get; set; }
    }
}
