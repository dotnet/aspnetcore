// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Security.Notifications
{
    public class MessageReceivedNotification<TMessage>
    {
        public MessageReceivedNotification()
        {
        }

        public bool Cancel { get; set; }
        public TMessage ProtocolMessage { get; set; }
    }
}