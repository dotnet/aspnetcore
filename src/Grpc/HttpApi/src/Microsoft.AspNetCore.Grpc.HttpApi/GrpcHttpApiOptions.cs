// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Grpc.HttpApi;

/// <summary>
/// Options used to configure gRPC HTTP API service instances.
/// </summary>
public class GrpcHttpApiOptions
{
    /// <summary>
    /// Gets or sets the <see cref="HttpApi.JsonSettings"/> used to serialize messages.
    /// </summary>
    public JsonSettings JsonSettings { get; set; } = new JsonSettings();
}
