// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Google.Protobuf.Reflection;
using Microsoft.AspNetCore.Grpc.HttpApi.Internal.Json;

namespace Microsoft.AspNetCore.Grpc.HttpApi;

// TODO - improve names. boolean property values should aim to be false
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class JsonSettings
{
    private readonly Lazy<JsonSerializerOptions> _unaryOptions;
    private readonly Lazy<JsonSerializerOptions> _serverStreamingOptions;

    public JsonSettings()
    {
        _unaryOptions = new Lazy<JsonSerializerOptions>(
            () => JsonConverterHelper.CreateSerializerOptions(this),
            LazyThreadSafetyMode.ExecutionAndPublication);
        _serverStreamingOptions = new Lazy<JsonSerializerOptions>(
            () => JsonConverterHelper.CreateSerializerOptions(this, isStreamingOptions: true),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>
    /// Whether fields which would otherwise not be included in the formatted data
    /// should be formatted even when the value is not present, or has the default value.
    /// This option only affects fields which don't support "presence" (e.g.
    /// singular non-optional proto3 primitive fields).
    /// </summary>
    public bool FormatDefaultValues { get; set; } = true;

    public bool FormatEnumsAsIntegers { get; set; }

    public bool FormatInt64sAsIntegers { get; set; } = true;

    public TypeRegistry TypeRegistry { get; set; } = TypeRegistry.Empty;

    public bool WriteIndented { get; set; } = true;

    internal JsonSerializerOptions UnarySerializerOptions => _unaryOptions.Value;
    internal JsonSerializerOptions ServerStreamingSerializerOptions => _serverStreamingOptions.Value;
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
