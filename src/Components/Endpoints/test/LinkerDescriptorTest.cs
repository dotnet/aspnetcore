// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Xml.Linq;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class LinkerDescriptorTest
{
    [Fact]
    public void SubstitutionsTargetEndpointsAssembly()
    {
        using var stream = typeof(JsonTempDataSerializer).Assembly
            .GetManifestResourceStream("ILLink.Substitutions.xml");

        Assert.NotNull(stream);
        var assembly = Assert.Single(XDocument.Load(stream).Root!.Elements("assembly"));
        Assert.Equal("Microsoft.AspNetCore.Components.Endpoints", (string?)assembly.Attribute("fullname"));
    }
}
