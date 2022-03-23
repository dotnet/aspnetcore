// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding;

/// <summary>
/// Options used to configure gRPC HTTP API service instances.
/// </summary>
public class GrpcJsonTranscodingOptions
{
    /// <summary>
    /// Gets or sets the <see cref="JsonTranscoding.JsonSettings"/> used to serialize messages.
    /// </summary>
    public JsonSettings JsonSettings { get; set; } = new JsonSettings();
}
