// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Grpc.Shared;
using Microsoft.AspNetCore.Grpc.JsonTranscoding;
using Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.Json;

namespace Microsoft.AspNetCore.Grpc.Microbenchmarks.Json;

public class LegacyParsing
{
    private const string TimestampJson = "\"2020-12-01T00:30:00.123456789+18:00\"";
    private const string DurationJson = "\"1234567890.123456789s\"";

    private JsonSerializerOptions _serializerOptions = default!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var descriptorRegistry = new DescriptorRegistry();
        descriptorRegistry.RegisterFileDescriptor(Timestamp.Descriptor.File);
        descriptorRegistry.RegisterFileDescriptor(Duration.Descriptor.File);

        _serializerOptions = JsonConverterHelper.CreateSerializerOptions(
            new JsonContext(new GrpcJsonSettings(), TypeRegistry.Empty, descriptorRegistry));
    }

    [Benchmark]
    public (long Seconds, int Nanos) ParseTimestamp() =>
        Legacy.ParseTimestamp("2020-12-01T00:30:00.123456789+18:00");

    [Benchmark]
    public (long Seconds, int Nanos) ParseDuration() =>
        Legacy.ParseDuration("1234567890.123456789s");

    [Benchmark]
    public Timestamp DeserializeTimestamp() =>
        JsonSerializer.Deserialize<Timestamp>(TimestampJson, _serializerOptions)!;

    [Benchmark]
    public Duration DeserializeDuration() =>
        JsonSerializer.Deserialize<Duration>(DurationJson, _serializerOptions)!;

    [Benchmark]
    public string WriteTimestamp() =>
        Legacy.GetTimestampText(123456789, 1606782600);

    [Benchmark]
    public bool ValidateFieldMask() =>
        Legacy.IsPathValid("foo_bar.baz");
}
