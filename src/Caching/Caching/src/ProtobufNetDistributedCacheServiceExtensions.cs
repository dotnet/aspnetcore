// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Caching.Distributed;

[SuppressMessage("ApiDesign", "RS0016:Add public types and members to the declared API", Justification = "demo code only")]
public static class ProtobufDistributedCacheServiceExtensions
{
    public static IServiceCollection AddHybridCacheSerializerProtobufNet(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IHybridCacheSerializerFactory, ProtobufNetSerializerFactory>();
        return services;
    }

    private sealed class ProtobufNetSerializerFactory : IHybridCacheSerializerFactory
    {
        public bool TryCreateSerializer<T>([NotNullWhen(true)] out IHybridCacheSerializer<T>? serializer)
        {
            // in real implementation, would use library rules
            if (Attribute.IsDefined(typeof(T), typeof(DataContractAttribute)))
            {
                serializer = new ProtobufNetSerializer<T>();
                return true;
            }
            serializer = null;
            return false;
        }
    }
    internal sealed class ProtobufNetSerializer<T> : IHybridCacheSerializer<T>
    {
        // in real implementation, would use library serializer
        public T Deserialize(ReadOnlySequence<byte> source) => throw new NotImplementedException();

        public void Serialize(T value, IBufferWriter<byte> target) => throw new NotImplementedException();
    }
}
