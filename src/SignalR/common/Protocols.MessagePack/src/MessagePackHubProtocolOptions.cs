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

        /// <summary>
        /// <para>Get or Set the <see cref="MessagePackSerializerOptions"/> used internally by the <see cref="MessagePackSerializer" />.</para>
        /// <para>If you override it, we strongly recommend that you set <see cref="MessagePackSecurity" /> to <see cref="MessagePackSecurity.UntrustedData"/> by calling:<para>
        /// <code>customMessagePackSerializerOptions.WithSecurity(MessagePackSecurity.UntrustedData)</code>
        /// If you want to modify to the default options you need to assign the options back to the <see cref="SerializerOptions" /> after modifications:
        /// <code>SerializerOptions = SerializerOptions.WithResolver(new CustomResolver());</code>
        /// </summary>
        public MessagePackSerializerOptions SerializerOptions
        {
            get
            {
                if (_messagePackSerializerOptions == null)
                {
                    // The default set of resolvers trigger a static constructor that throws on AOT environments.
                    // This gives users the chance to use an AOT friendly formatter.
                    _messagePackSerializerOptions = MessagePackHubProtocol.CreateDefaultMessagePackSerializerOptions();
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
