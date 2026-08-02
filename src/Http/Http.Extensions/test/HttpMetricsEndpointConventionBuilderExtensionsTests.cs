// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;

namespace Microsoft.AspNetCore.Http.Extensions.Tests;

public partial class HttpMetricsEndpointConventionBuilderExtensionsTests
{
    [Fact]
    public void DisableHttpMetrics_AddsMetadata()
    {
        var builder = new TestEndointConventionBuilder();
        builder.DisableHttpMetrics();

        Assert.IsAssignableFrom<IDisableHttpMetricsMetadata>(Assert.Single(builder.Metadata));
    }

    private sealed class TestEndointConventionBuilder : EndpointBuilder, IEndpointConventionBuilder
    {
        public void Add(Action<EndpointBuilder> convention)
        {
            convention(this);
        }

        public override Endpoint Build() => throw new NotImplementedException();
    }
}
