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
    public static IServiceCollection AddReadThroughCacheSerializerProtobufNet(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IReadThroughCacheSerializerFactory, ProtobufNetSerializerFactory>();
        return services;
    }

    private sealed class ProtobufNetSerializerFactory : IReadThroughCacheSerializerFactory
    {
        public bool TryCreateSerializer<T>([NotNullWhen(true)] out IReadThroughCacheSerializer<T>? serializer)
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
    internal sealed class ProtobufNetSerializer<T> : IReadThroughCacheSerializer<T>
    {
        // in real implementation, would use library serializer
        public T Deserialize(ReadOnlySequence<byte> source) => throw new NotImplementedException();

        public void Serialize(T value, IBufferWriter<byte> target) => throw new NotImplementedException();
    }
}
