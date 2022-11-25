// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Google.Protobuf.Reflection;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.Json;

internal sealed class JsonContext
{
    public JsonContext(GrpcJsonSettings settings, TypeRegistry typeRegistry, DescriptorRegistry serviceDescriptorRegistry)
    {
        Settings = settings;
        TypeRegistry = typeRegistry;
        ServiceDescriptorRegistry = serviceDescriptorRegistry;
    }

    public GrpcJsonSettings Settings { get; }
    public TypeRegistry TypeRegistry { get; }
    public DescriptorRegistry ServiceDescriptorRegistry { get; }
}
