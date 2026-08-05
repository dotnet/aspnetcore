// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Components.Web;

public class AssetPathAttributesTest
{
    [Fact]
    public void DeclaresBuiltInAssetPathMappings()
    {
        var mappings = typeof(AssetPathAttributes)
            .GetCustomAttributes<AcceptsAssetPathAttribute>()
            .Select(attribute => (attribute.ElementName, attribute.AttributeName))
            .OrderBy(mapping => mapping.ElementName)
            .ToArray();

        Assert.Equal(
            [
                ("img", "src"),
                ("link", "href"),
                ("script", "src"),
            ],
            mappings);
    }
}
