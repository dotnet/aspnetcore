// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

public class OpenApiExternalDocsComparerTests
{
    public static object[][] Data => [
        [new OpenApiExternalDocs(), new OpenApiExternalDocs(), true],
        [new OpenApiExternalDocs(), new OpenApiExternalDocs { Description = "description" }, false],
        [new OpenApiExternalDocs { Description = "description" }, new OpenApiExternalDocs { Description = "description" }, true],
        [new OpenApiExternalDocs { Description = "description" }, new OpenApiExternalDocs { Description = "description", Url = new Uri("http://localhost") }, false],
        [new OpenApiExternalDocs { Description = "description", Url = new Uri("http://localhost") }, new OpenApiExternalDocs { Description = "description", Url = new Uri("http://localhost") }, true],
        [new OpenApiExternalDocs { Description = "description", Url = new Uri("http://localhost") }, new OpenApiExternalDocs { Description = "description", Url = new Uri("http://localhost") }, true],
    ];

    [Theory]
    [MemberData(nameof(Data))]
    public void ProducesCorrectEqualityForOpenApiExternalDocs(OpenApiExternalDocs externalDocs, OpenApiExternalDocs anotherExternalDocs, bool isEqual)
        => Assert.Equal(isEqual, OpenApiExternalDocsComparer.Instance.Equals(externalDocs, anotherExternalDocs));
}
