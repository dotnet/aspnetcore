// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Security.Notifications
{
    public class MessageReceivedNotification<TMessage, TOptions> : BaseNotification<TOptions>
    {
        public MessageReceivedNotification(HttpContext context, TOptions options) : base(context, options)
        {
        }

        public TMessage ProtocolMessage { get; set; }
    }
}