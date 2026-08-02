// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SignalR.Protocol;

/// <summary>
/// Implements the SignalR Hub Protocol using MessagePack.
/// </summary>
public class MessagePackHubProtocol : IHubProtocol
{
    private const string ProtocolName = "messagepack";
    private const int ProtocolVersion = 2;
    private readonly DefaultMessagePackHubProtocolWorker _worker;

    /// <inheritdoc />
    public string Name => ProtocolName;

    /// <inheritdoc />
    public int Version => ProtocolVersion;

    /// <inheritdoc />
    public TransferFormat TransferFormat => TransferFormat.Binary;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagePackHubProtocol"/> class.
    /// </summary>
    public MessagePackHubProtocol()
        : this(Options.Create(new MessagePackHubProtocolOptions()))
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagePackHubProtocol"/> class.
    /// </summary>
    /// <param name="options">The options used to initialize the protocol.</param>
    public MessagePackHubProtocol(IOptions<MessagePackHubProtocolOptions> options)
    {
        ArgumentNullThrowHelper.ThrowIfNull(options);

        _worker = new DefaultMessagePackHubProtocolWorker(options.Value.SerializerOptions);
    }

    /// <inheritdoc />
    public bool IsVersionSupported(int version)
    {
        return version <= Version;
    }

    /// <inheritdoc />
    public bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, [NotNullWhen(true)] out HubMessage? message)
        => _worker.TryParseMessage(ref input, binder, out message);

    /// <inheritdoc />
    public void WriteMessage(HubMessage message, IBufferWriter<byte> output)
        => _worker.WriteMessage(message, output);

    /// <inheritdoc />
    public ReadOnlyMemory<byte> GetMessageBytes(HubMessage message)
        => _worker.GetMessageBytes(message);

    internal static MessagePackSerializerOptions CreateDefaultMessagePackSerializerOptions() =>
        MessagePackSerializerOptions
            .Standard
            .WithResolver(SignalRResolver.Instance)
            .WithSecurity(MessagePackSecurity.UntrustedData);

    internal sealed class SignalRResolver : IFormatterResolver
    {
        public static readonly IFormatterResolver Instance = new SignalRResolver();

        public static readonly IReadOnlyList<IFormatterResolver> Resolvers = new IFormatterResolver[]
        {
                DynamicEnumAsStringResolver.Instance,
                ContractlessStandardResolver.Instance,
        };

        public IMessagePackFormatter<T>? GetFormatter<T>()
        {
            return Cache<T>.Formatter;
        }

        private static class Cache<T>
        {
            public static readonly IMessagePackFormatter<T>? Formatter = ResolveFormatter();

            private static IMessagePackFormatter<T>? ResolveFormatter()
            {
                foreach (var resolver in Resolvers)
                {
                    var formatter = resolver.GetFormatter<T>();
                    if (formatter != null)
                    {
                        return formatter;
                    }
                }

                return null;
            }
        }
    }
}
