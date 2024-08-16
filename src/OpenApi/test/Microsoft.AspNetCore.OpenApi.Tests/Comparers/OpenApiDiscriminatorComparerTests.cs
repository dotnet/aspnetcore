// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

public class OpenApiDiscriminatorComparerTests
{
    public static object[][] Data => [
        [new OpenApiDiscriminator(), new OpenApiDiscriminator(), true],
        [new OpenApiDiscriminator { PropertyName = "prop" }, new OpenApiDiscriminator(), false],
        [new OpenApiDiscriminator { PropertyName = "prop" }, new OpenApiDiscriminator { PropertyName = "prop" }, true],
        [new OpenApiDiscriminator { PropertyName = "prop2" }, new OpenApiDiscriminator { PropertyName = "prop" }, false],
        [new OpenApiDiscriminator { PropertyName = "prop", Mapping = { ["key"] = "discriminatorValue" } }, new OpenApiDiscriminator { PropertyName = "prop", Mapping = { ["key"] = "discriminatorValue" } }, true],
        [new OpenApiDiscriminator { PropertyName = "prop", Mapping = { ["key"] = "discriminatorValue" } }, new OpenApiDiscriminator { PropertyName = "prop2", Mapping = { ["key"] = "discriminatorValue" } }, false],
        [new OpenApiDiscriminator { PropertyName = "prop", Mapping = { ["key"] = "discriminatorValue" } }, new OpenApiDiscriminator { PropertyName = "prop", Mapping = { ["key"] = "discriminatorValue2" } }, false]
    ];

    [Theory]
    [MemberData(nameof(Data))]
    public void ProducesCorrectEqualityForOpenApiDiscriminator(OpenApiDiscriminator discriminator, OpenApiDiscriminator anotherDiscriminator, bool isEqual)
        => Assert.Equal(isEqual, OpenApiDiscriminatorComparer.Instance.Equals(discriminator, anotherDiscriminator));
}
