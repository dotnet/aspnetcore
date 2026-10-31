// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using MessagePack;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR;

/// <summary>
/// The <see cref="MessagePackHubProtocol"/> options.
/// </summary>
public class MessagePackHubProtocolOptions
{
    private MessagePackSerializerOptions? _messagePackSerializerOptions;

    /// <summary>
    /// <para>Gets or sets the <see cref="MessagePackSerializerOptions"/> used internally by the <see cref="MessagePackSerializer" />.</para>
    /// <para>If you override the default value, we strongly recommend that you set <see cref="MessagePackSecurity" /> to <see cref="MessagePackSecurity.UntrustedData"/> by calling:</para>
    /// <code>customMessagePackSerializerOptions = customMessagePackSerializerOptions.WithSecurity(MessagePackSecurity.UntrustedData)</code>
    /// If you modify the default options you must also assign the updated options back to the <see cref="SerializerOptions" /> property:
    /// <code>options.SerializerOptions = options.SerializerOptions.WithResolver(new CustomResolver());</code>
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
