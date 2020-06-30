// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using MessagePack;

namespace Microsoft.AspNetCore.SignalR.Protocol
{
    internal sealed class DefaultMessagePackHubProtocolWorker : MessagePackHubProtocolWorker
    {
        private readonly MessagePackSerializerOptions _messagePackSerializerOptions;

        public DefaultMessagePackHubProtocolWorker(MessagePackSerializerOptions messagePackSerializerOptions)
        {
            _messagePackSerializerOptions = messagePackSerializerOptions;
        }

        protected override object DeserializeObject(ref MessagePackReader reader, Type type, string field)
        {
            try
            {
                return MessagePackSerializer.Deserialize(type, ref reader, _messagePackSerializerOptions);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Deserializing object of the `{type.Name}` type for '{field}' failed.", ex);
            }
        }

        protected override void Serialize(ref MessagePackWriter writer, Type type, object value)
        {
            MessagePackSerializer.Serialize(type, ref writer, value, _messagePackSerializerOptions);
        }
    }
}
