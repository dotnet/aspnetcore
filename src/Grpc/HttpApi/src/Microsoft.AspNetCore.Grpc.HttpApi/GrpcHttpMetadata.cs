// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Google.Api;
using Google.Protobuf.Reflection;

namespace Microsoft.AspNetCore.Grpc.HttpApi;

/// <summary>
/// Metadata for a gRPC HTTP API endpoint.
/// </summary>
public class GrpcHttpMetadata
{
    /// <summary>
    /// Creates a new instance of <see cref="GrpcHttpMetadata"/> with the provided Protobuf
    /// <see cref="Google.Protobuf.Reflection.MethodDescriptor"/> and <see cref="Google.Api.HttpRule"/>.
    /// </summary>
    /// <param name="methodDescriptor">The Protobuf <see cref="Google.Protobuf.Reflection.MethodDescriptor"/>.</param>
    /// <param name="httpRule">The <see cref="Google.Api.HttpRule"/>.</param>
    public GrpcHttpMetadata(MethodDescriptor methodDescriptor, HttpRule httpRule)
    {
        MethodDescriptor = methodDescriptor;
        HttpRule = httpRule;
    }

    /// <summary>
    /// Gets the Protobuf <see cref="Google.Protobuf.Reflection.MethodDescriptor"/>.
    /// </summary>
    public MethodDescriptor MethodDescriptor { get; }

    /// <summary>
    /// Gets the <see cref="Google.Api.HttpRule"/>.
    /// </summary>
    public HttpRule HttpRule { get; }
}
