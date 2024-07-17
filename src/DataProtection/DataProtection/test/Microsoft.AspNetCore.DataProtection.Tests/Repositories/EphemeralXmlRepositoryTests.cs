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

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void DeleteElements(bool delete1, bool delete2)
    {
        var repository = new EphemeralXmlRepository(NullLoggerFactory.Instance);

        var element1 = new XElement("element1");
        var element2 = new XElement("element2");

        repository.StoreElement(element1, friendlyName: null);
        repository.StoreElement(element2, friendlyName: null);

        var ranSelector = false;

        Assert.True(repository.DeleteElements(deletableElements =>
        {
            ranSelector = true;
            Assert.Equal(2, deletableElements.Count);

            foreach (var element in deletableElements)
            {
                switch (element.Element.Name.LocalName)
                {
                    case "element1":
                        element.DeletionOrder = delete1 ? 1 : null;
                        break;
                    case "element2":
                        element.DeletionOrder = delete2 ? 2 : null;
                        break;
                    default:
                        Assert.Fail("Unexpected element name: " + element.Element.Name.LocalName);
                        break;
                }
            }
        }));
        Assert.True(ranSelector);

        var elementSet = new HashSet<string>(repository.GetAllElements().Select(e => e.Name.LocalName));

        Assert.InRange(elementSet.Count, 0, 2);

        Assert.Equal(!delete1, elementSet.Contains(element1.Name.LocalName));
        Assert.Equal(!delete2, elementSet.Contains(element2.Name.LocalName));
    }

    [Fact]
    public void DeleteElementsWithOutOfBandDeletion()
    {
        var repository = new EphemeralXmlRepository(NullLoggerFactory.Instance);

        repository.StoreElement(new XElement("element1"), friendlyName: "friendly1");

        var ranSelector = false;

        Assert.True(repository.DeleteElements(deletableElements =>
        {
            ranSelector = true;

            // Now that the repository has read the element from the registry, delete it out-of-band.
            repository.DeleteElements(deletableElements => deletableElements.First().DeletionOrder = 1);

            Assert.Equal(1, deletableElements.Count);

            deletableElements.First().DeletionOrder = 1;
        }));
        Assert.True(ranSelector);

        Assert.Empty(repository.GetAllElements());
    }
}
