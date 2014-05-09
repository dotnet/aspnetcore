// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;

namespace Microsoft.AspNet.Security.Notifications
{
    public class AuthenticationFailedNotification<TMessage>
    {
        public AuthenticationFailedNotification()
        {
        }

        public bool Cancel { get; set; }
        public Exception Exception { get; set; }
        public TMessage ProtocolMessage { get; set; }
    }
}