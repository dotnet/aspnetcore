// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Metadata;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Specifies that HTTP request duration metrics is disabled for an endpoint.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class DisableHttpMetricsAttribute : Attribute, IDisableHttpMetricsMetadata
{
}
