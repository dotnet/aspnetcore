// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Security.Notifications
{
    public class SecurityTokenValidatedNotification
    {
        public SecurityTokenValidatedNotification()
        {
        }

        public AuthenticationTicket AuthenticationTicket { get; set; }
        public bool Cancel { get; set; }
    }
}