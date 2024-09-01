// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

public class OpenApiReferenceComparerTests
{
    public static object[][] Data => [
        [new OpenApiReference(), new OpenApiReference(), true],
        [new OpenApiReference(), new OpenApiReference { Id = "id" }, false],
        [new OpenApiReference { Id = "id" }, new OpenApiReference { Id = "id" }, true],
        [new OpenApiReference { Id = "id" }, new OpenApiReference { Id = "id", Type = ReferenceType.Schema }, false],
        [new OpenApiReference { Id = "id", Type = ReferenceType.Schema }, new OpenApiReference { Id = "id", Type = ReferenceType.Schema }, true],
        [new OpenApiReference { Id = "id", Type = ReferenceType.Schema }, new OpenApiReference { Id = "id", Type = ReferenceType.Response }, false],
        [new OpenApiReference { Id = "id", Type = ReferenceType.Response, ExternalResource = "http://localhost/pet.json" }, new OpenApiReference { Id = "id", Type = ReferenceType.Response, ExternalResource = "http://localhost/pet.json" }, true],
        [new OpenApiReference { Id = "id", Type = ReferenceType.Response, ExternalResource = "http://localhost/pet.json" }, new OpenApiReference { Id = "id", Type = ReferenceType.Response, ExternalResource = "http://localhost/pet2.json" }, false],
        [new OpenApiReference { Id = "id", Type = ReferenceType.Response, ExternalResource = "http://localhost/pet.json", HostDocument = new OpenApiDocument() }, new OpenApiReference { Id = "id", Type = ReferenceType.Response, ExternalResource = "http://localhost/pet.json", HostDocument = new OpenApiDocument() }, true],
        [new OpenApiReference { Id = "id", Type = ReferenceType.Response, ExternalResource = "http://localhost/pet.json", HostDocument = new OpenApiDocument { Info = new() { Title = "Test" }} }, new OpenApiReference { Id = "id", Type = ReferenceType.Response, ExternalResource = "http://localhost/pet2.json", HostDocument = new OpenApiDocument() }, false]
    ];

    [Theory]
    [MemberData(nameof(Data))]
    public void ProducesCorrectEqualityForOpenApiReference(OpenApiReference reference, OpenApiReference anotherReference, bool isEqual)
        => Assert.Equal(isEqual, OpenApiReferenceComparer.Instance.Equals(reference, anotherReference));
}
