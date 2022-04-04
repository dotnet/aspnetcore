// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Http;

public class EndpointParameterMetadataContext
{
    public ParameterInfo Parameter { get; internal set; }

    public IServiceProvider? Services { get; init; }

    public IList<object> EndpointMetadata { get; init; }
}
