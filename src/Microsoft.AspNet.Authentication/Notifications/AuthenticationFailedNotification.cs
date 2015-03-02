// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Authentication.Notifications
{
    public class AuthenticationFailedNotification<TMessage, TOptions> : BaseNotification<TOptions>
    {
        public AuthenticationFailedNotification(HttpContext context, TOptions options) : base(context, options)
        {
        }

        public Exception Exception { get; set; }

        public TMessage ProtocolMessage { get; set; }
    }
}