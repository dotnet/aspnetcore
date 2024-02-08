// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Caching.Distributed;

public static class DistributedCacheServiceExtensions
{
    public static IServiceCollection AddAdvancedDistributedCache(this IServiceCollection services, Action<TypedDistributedCacheOptions> setupAction)
    {
        ArgumentNullException.ThrowIfNull(setupAction);
        AddAdvancedDistributedCache(services);
        services.Configure(setupAction);
        return services;
    }

    public static IServiceCollection AddAdvancedDistributedCache(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddOptions();
        services.AddDistributedMemoryCache(); // we need a backend; use in-proc by default
        services.AddSingleton(typeof(ICacheSerializer<>), typeof(DefaultJsonSerializer<>));
        services.AddSingleton<ICacheSerializer<string>, StringSerializer>();
        services.AddSingleton(typeof(IAdvancedDistributedCache), typeof(AdvancedDistributedCache));
        return services;
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0016:Add public types and members to the declared API", Justification = "<Pending>")]
public static class ProtobufDistributedCacheServiceExtensions
{
    public static IServiceCollection AddCacheSerializerProtobufNet(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton(typeof(ICacheSerializer<>), typeof(ProtobufNetSerializer<>));
        return services;
    }

    public class ProtobufNetSerializer<T> : ICacheSerializer<T>
    {
        public bool IsSupported => Attribute.IsDefined(typeof(T), typeof(DataContractAttribute));

        public T Deserialize(ReadOnlySequence<byte> source) => throw new NotImplementedException();

        public void Serialize(T value, IBufferWriter<byte> target) => throw new NotImplementedException();
    }
}

public sealed class TypedDistributedCacheOptions
{
    // TBD
}

