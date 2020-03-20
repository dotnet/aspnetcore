// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using MessagePack;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR
{
    public class MessagePackHubProtocolOptions
    {
        private MessagePackSerializerOptions _messagePackSerializerOptions;

        public MessagePackSerializerOptions SerializerOptions
        {
            get
            {
                if (_messagePackSerializerOptions == null)
                {
                    // The default set of resolvers trigger a static constructor that throws on AOT environments.
                    // This gives users the chance to use an AOT friendly formatter.
                    _messagePackSerializerOptions = MessagePackHubProtocol.CreateDefaultFormatterResolvers();
                }

                return _messagePackSerializerOptions;
            }
            set
            {
                _messagePackSerializerOptions = value;
            }
        }
    }
}
