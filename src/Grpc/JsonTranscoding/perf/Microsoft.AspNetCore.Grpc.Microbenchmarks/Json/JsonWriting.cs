// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Greet;
using Grpc.Shared;
using Microsoft.AspNetCore.Grpc.JsonTranscoding;
using Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.Json;

namespace Microsoft.AspNetCore.Grpc.Microbenchmarks.Json;

public class JsonWriting
{
    private HelloRequest _request = default!;
    private JsonSerializerOptions _serializerOptions = default!;
    private JsonFormatter _jsonFormatter = default!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var descriptorRegistry = new DescriptorRegistry();
        descriptorRegistry.RegisterFileDescriptor(HelloRequest.Descriptor.File);

        _request = new HelloRequest() { Name = "Hello world" };
        _serializerOptions = JsonConverterHelper.CreateSerializerOptions(
            new JsonContext(new GrpcJsonSettings { WriteIndented = false }, TypeRegistry.Empty, descriptorRegistry));
        _jsonFormatter = new JsonFormatter(new JsonFormatter.Settings(formatDefaultValues: false));
    }

    [Benchmark]
    public void WriteMessage_JsonSerializer()
    {
        JsonSerializer.Serialize(_request, _serializerOptions);
    }

    [Benchmark]
    public void WriteMessage_JsonFormatter()
    {
        _jsonFormatter.Format(_request);
    }
}
