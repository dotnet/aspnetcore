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

public class JsonReading
{
    private string _requestJson = default!;
    private JsonSerializerOptions _serializerOptions = default!;
    private JsonParser _jsonFormatter = default!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var descriptorRegistry = new DescriptorRegistry();
        descriptorRegistry.RegisterFileDescriptor(HelloRequest.Descriptor.File);

        _requestJson = (new HelloRequest() { Name = "Hello world" }).ToString();
        _serializerOptions = JsonConverterHelper.CreateSerializerOptions(new JsonContext(new GrpcJsonSettings { WriteIndented = false }, TypeRegistry.Empty, descriptorRegistry));
        _jsonFormatter = new JsonParser(new JsonParser.Settings(recursionLimit: 100));
    }

    [Benchmark]
    public void ReadMessage_JsonSerializer()
    {
        JsonSerializer.Deserialize(_requestJson, typeof(HelloRequest), _serializerOptions);
    }

    [Benchmark]
    public void ReadMessage_JsonFormatter()
    {
        _jsonFormatter.Parse(_requestJson, HelloRequest.Descriptor);
    }
}
