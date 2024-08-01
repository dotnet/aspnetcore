// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

public class OpenApiXmlComparerTests
{
    public static object[][] Data => [
        [new OpenApiXml(), new OpenApiXml(), true],
        [new OpenApiXml(), new OpenApiXml { Name = "name" }, false],
        [new OpenApiXml { Name = "name" }, new OpenApiXml { Name = "name" }, true],
        [new OpenApiXml { Name = "name" }, new OpenApiXml { Name = "name", Namespace = new Uri("http://localhost.com/namespace") }, false],
        [new OpenApiXml { Name = "name", Namespace = new Uri("http://localhost.com/namespace") }, new OpenApiXml { Name = "name", Namespace = new Uri("http://localhost.com/namespace") }, true],
        [new OpenApiXml { Name = "name", Namespace = new Uri("http://localhost.com/namespace") }, new OpenApiXml { Name = "name", Namespace = new Uri("http://localhost.com/namespace2") }, false],
        [new OpenApiXml { Name = "name", Namespace = new Uri("http://localhost.com/namespace"), Prefix = "prefix" }, new OpenApiXml { Name = "name", Namespace = new Uri("http://localhost.com/namespace"), Prefix = "prefix" }, true],
        [new OpenApiXml { Name = "name", Namespace = new Uri("http://localhost.com/namespace"), Prefix = "prefix" }, new OpenApiXml { Name = "name", Namespace = new Uri("http://localhost.com/namespace"), Prefix = "prefix2" }, false]
    ];

    [Theory]
    [MemberData(nameof(Data))]
    public void ProducesCorrectEqualityForOpenApiXml(OpenApiXml xml, OpenApiXml anotherXml, bool isEqual)
        => Assert.Equal(isEqual, OpenApiXmlComparer.Instance.Equals(xml, anotherXml));
}
