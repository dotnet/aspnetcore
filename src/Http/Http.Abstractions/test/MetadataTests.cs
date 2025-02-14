// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Metadata;

namespace Microsoft.AspNetCore.Http;

public class MetadataTests
{
    [Fact]
    public void ProducesResponseTypeMetadata_ToString()
    {
        // Act and Assert
        Assert.Equal("Produces StatusCode: 100", new ProducesResponseTypeMetadata(100).ToString());
        Assert.Equal("Produces StatusCode: 200, ContentTypes: application/json", new ProducesResponseTypeMetadata(200, contentTypes: new[] { "application/json" }).ToString());
        Assert.Equal("Produces StatusCode: 300, ContentTypes: application/json,text/plain", new ProducesResponseTypeMetadata(300, contentTypes: new[] { "application/json", "text/plain" }).ToString());
        Assert.Equal("Produces StatusCode: 400, Type: System.Version", new ProducesResponseTypeMetadata(400, type: typeof(Version)).ToString());
        Assert.Equal("Produces StatusCode: 500, Type: System.Void", new ProducesResponseTypeMetadata(500, type: typeof(void)).ToString());
    }

    [Fact]
    public void AcceptsMetadata_ToString()
    {
        // Act and Assert
        Assert.Equal("Accepts ContentTypes: application/json, IsOptional: False", new AcceptsMetadata(new[] { "application/json" }).ToString());
        Assert.Equal("Accepts ContentTypes: application/json,text/plain, IsOptional: False", new AcceptsMetadata(new[] { "application/json", "text/plain" }).ToString());
        Assert.Equal("Accepts ContentTypes: application/json, RequestType: System.Version, IsOptional: False", new AcceptsMetadata(new[] { "application/json" }, type: typeof(Version)).ToString());
        Assert.Equal("Accepts ContentTypes: application/json, IsOptional: True", new AcceptsMetadata(new[] { "application/json" }, isOptional: true).ToString());
    }
}
