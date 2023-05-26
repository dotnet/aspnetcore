// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.DataProtection.Repositories;

public class EphemeralXmlRepositoryTests
{
    [Fact]
    public void GetAllElements_Empty()
    {
        // Arrange
        var repository = new EphemeralXmlRepository(NullLoggerFactory.Instance);

        // Act & assert
        Assert.Empty(repository.GetAllElements());
    }

    [Fact]
    public void Store_Then_Get()
    {
        // Arrange
        var element1 = XElement.Parse(@"<element1 />");
        var element2 = XElement.Parse(@"<element1 />");
        var element3 = XElement.Parse(@"<element1 />");
        var repository = new EphemeralXmlRepository(NullLoggerFactory.Instance);

        // Act & assert
        repository.StoreElement(element1, "Invalid friendly name."); // nobody should care about the friendly name
        repository.StoreElement(element2, "abcdefg");
        Assert.Equal(new[] { element1, element2 }, repository.GetAllElements(), XmlAssert.EqualityComparer);
        repository.StoreElement(element3, null);
        Assert.Equal(new[] { element1, element2, element3 }, repository.GetAllElements(), XmlAssert.EqualityComparer);
    }
}
