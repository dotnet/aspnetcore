// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Grpc.Shared;
using Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.Json;
using Transcoding;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Tests.ConverterTests;

public class MessageTypeInfoResolverTests
{
    [Fact]
    public void GetTypeInfo_IMessage_Null()
    {
        var resolver = CreateResolver();

        Assert.Null(resolver.GetTypeInfo(typeof(IMessage), new JsonSerializerOptions()));
    }

    [Fact]
    public void GetTypeInfo_IMessageOfT_Null()
    {
        var resolver = CreateResolver();

        Assert.Null(resolver.GetTypeInfo(typeof(IMessage<HelloRequest>), new JsonSerializerOptions()));
    }

    [Fact]
    public void GetTypeInfo_IBufferMessage_Null()
    {
        var resolver = CreateResolver();

        Assert.Null(resolver.GetTypeInfo(typeof(IBufferMessage), new JsonSerializerOptions()));
    }

    [Fact]
    public void GetTypeInfo_HelloRequest_Success()
    {
        var descriptorRegistry = new DescriptorRegistry();
        descriptorRegistry.RegisterFileDescriptor(HelloRequest.Descriptor.File);

        var resolver = CreateResolver(descriptorRegistry);

        var typeInfo = resolver.GetTypeInfo(typeof(HelloRequest), new JsonSerializerOptions());
        Assert.NotNull(typeInfo);

        Assert.NotEmpty(typeInfo.Properties);
    }

    private static MessageTypeInfoResolver CreateResolver(DescriptorRegistry? descriptorRegistry = null)
    {
        var context = new JsonContext(new GrpcJsonSettings(), TypeRegistry.Empty, descriptorRegistry ?? new DescriptorRegistry());
        return new MessageTypeInfoResolver(context);
    }
}
