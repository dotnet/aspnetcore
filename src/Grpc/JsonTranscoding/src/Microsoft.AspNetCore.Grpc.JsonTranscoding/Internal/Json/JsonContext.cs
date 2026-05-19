// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Google.Protobuf.Reflection;
using Grpc.Shared;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.Json;

internal sealed class JsonContext
{
    public JsonContext(GrpcJsonSettings settings, TypeRegistry typeRegistry, DescriptorRegistry descriptorRegistry)
    {
        Settings = settings;
        TypeRegistry = typeRegistry;
        DescriptorRegistry = descriptorRegistry;
    }

    public GrpcJsonSettings Settings { get; }
    public TypeRegistry TypeRegistry { get; }
    public DescriptorRegistry DescriptorRegistry { get; }
}
