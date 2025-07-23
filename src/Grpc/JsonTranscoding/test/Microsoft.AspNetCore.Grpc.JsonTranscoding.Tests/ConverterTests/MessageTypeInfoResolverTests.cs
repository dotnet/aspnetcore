// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Grpc.Shared;
using Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.Json;
using Microsoft.AspNetCore.Grpc.JsonTranscoding.Tests.TestObjects.ProtobutMessages;
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

    //[Fact]
    //public void GetTypeInfo_StringValuesMessage_Success()
    //{
    //    var descriptorRegistry = new DescriptorRegistry();
    //    descriptorRegistry.RegisterFileDescriptor(StringValuesMessage.Descriptor.File);

    //    var resolver = CreateResolver(descriptorRegistry);

    //    var typeInfo = resolver.GetTypeInfo(typeof(StringValuesMessage), new JsonSerializerOptions());
    //    Assert.NotNull(typeInfo);

    //    Assert.NotEmpty(typeInfo.Properties);

    //    var message = new StringValuesMessage();
    //    message.Message = new StringValue { Value = "test" };

    //    var prop = Assert.Single(typeInfo.Properties);

    //    var value = prop.Get(message);
    //}

    private static MessageTypeInfoResolver CreateResolver(DescriptorRegistry? descriptorRegistry = null)
    {
        var context = new JsonContext(new GrpcJsonSettings(), TypeRegistry.Empty, descriptorRegistry ?? new DescriptorRegistry());
        return new MessageTypeInfoResolver(context);
    }
}
