// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Google.Protobuf.Reflection;
using Grpc.Shared;
using Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.Json;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding;

/// <summary>
/// Options used to configure gRPC JSON transcoding service instances.
/// </summary>
public sealed class GrpcJsonTranscodingOptions
{
    private readonly Lazy<JsonSerializerOptions> _unaryOptions;
    private readonly Lazy<JsonSerializerOptions> _serverStreamingOptions;

    public GrpcJsonTranscodingOptions()
    {
        _unaryOptions = new Lazy<JsonSerializerOptions>(
            () => JsonConverterHelper.CreateSerializerOptions(new JsonContext(JsonSettings, TypeRegistry, DescriptorRegistry)),
            LazyThreadSafetyMode.ExecutionAndPublication);
        _serverStreamingOptions = new Lazy<JsonSerializerOptions>(
            () => JsonConverterHelper.CreateSerializerOptions(new JsonContext(JsonSettings, TypeRegistry, DescriptorRegistry), isStreamingOptions: true),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    internal JsonSerializerOptions UnarySerializerOptions => _unaryOptions.Value;
    internal JsonSerializerOptions ServerStreamingSerializerOptions => _serverStreamingOptions.Value;

    // Registry is set by DI during startup.
    internal DescriptorRegistry DescriptorRegistry { get; set; } = default!;

    /// <summary>
    /// Gets or sets the <see cref="Google.Protobuf.Reflection.TypeRegistry"/> used to lookup types from type names.
    /// </summary>
    public TypeRegistry TypeRegistry { get; set; } = TypeRegistry.Empty;

    /// <summary>
    /// Gets or sets the <see cref="GrpcJsonSettings"/> used to serialize messages.
    /// </summary>
    public GrpcJsonSettings JsonSettings { get; set; } = new GrpcJsonSettings();
}
