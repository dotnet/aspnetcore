// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Win32;

namespace Microsoft.AspNetCore.DataProtection.Repositories;

public class RegistryXmlRepositoryTests
{
    [ConditionalFact]
    [ConditionalRunTestOnlyIfHkcuRegistryAvailable]
    public void RegistryKey_Property()
    {
        WithUniqueTempRegKey(regKey =>
        {
            // Arrange
            var repository = new RegistryXmlRepository(regKey, NullLoggerFactory.Instance);

            // Act
            var retVal = repository.RegistryKey;

            // Assert
            Assert.Equal(regKey, retVal);
        });
    }

    [ConditionalFact]
    [ConditionalRunTestOnlyIfHkcuRegistryAvailable]
    public void GetAllElements_EmptyOrNonexistentDirectory_ReturnsEmptyCollection()
    {
        WithUniqueTempRegKey(regKey =>
        {
            // Arrange
            var repository = new RegistryXmlRepository(regKey, NullLoggerFactory.Instance);

            // Act
            var allElements = repository.GetAllElements();

            // Assert
            Assert.Empty(allElements);
        });
    }

    [ConditionalFact]
    [ConditionalRunTestOnlyIfHkcuRegistryAvailable]
    public void StoreElement_WithValidFriendlyName_UsesFriendlyName()
    {
        WithUniqueTempRegKey(regKey =>
        {
            // Arrange
            var element = XElement.Parse("<element1 />");
            var repository = new RegistryXmlRepository(regKey, NullLoggerFactory.Instance);

            // Act
            repository.StoreElement(element, "valid-friendly-name");

            // Assert
            var valueNames = regKey.GetValueNames();
            var valueName = valueNames.Single(); // only one value should've been created

            // value name should be "valid-friendly-name"
            Assert.Equal("valid-friendly-name", valueName, StringComparer.OrdinalIgnoreCase);

            // value contents should be "<element1 />"
            var parsedElement = XElement.Parse(regKey.GetValue(valueName) as string);
            XmlAssert.Equal("<element1 />", parsedElement);
        });
    }

    [ConditionalTheory]
    [ConditionalRunTestOnlyIfHkcuRegistryAvailable]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("..")]
    [InlineData("not*friendly")]
    public void StoreElement_WithInvalidFriendlyName_CreatesNewGuidAsName(string friendlyName)
    {
        WithUniqueTempRegKey(regKey =>
        {
            // Arrange
            var element = XElement.Parse("<element1 />");
            var repository = new RegistryXmlRepository(regKey, NullLoggerFactory.Instance);

            // Act
            repository.StoreElement(element, friendlyName);

            // Assert
            var valueNames = regKey.GetValueNames();
            var valueName = valueNames.Single(); // only one value should've been created

            // value name should be "{GUID}"
            Guid parsedGuid = Guid.Parse(valueName as string, CultureInfo.InvariantCulture);
            Assert.NotEqual(Guid.Empty, parsedGuid);

            // value contents should be "<element1 />"
            var parsedElement = XElement.Parse(regKey.GetValue(valueName) as string);
            XmlAssert.Equal("<element1 />", parsedElement);
        });
    }

    [ConditionalFact]
    [ConditionalRunTestOnlyIfHkcuRegistryAvailable]
    public void StoreElements_ThenRetrieve_SeesAllElements()
    {
        WithUniqueTempRegKey(regKey =>
        {
            // Arrange
            var repository = new RegistryXmlRepository(regKey, NullLoggerFactory.Instance);

            // Act
            repository.StoreElement(new XElement("element1"), friendlyName: null);
            repository.StoreElement(new XElement("element2"), friendlyName: null);
            repository.StoreElement(new XElement("element3"), friendlyName: null);
            var allElements = repository.GetAllElements();

            // Assert
            var orderedNames = allElements.Select(el => el.Name.LocalName).OrderBy(name => name);
            Assert.Equal(new[] { "element1", "element2", "element3" }, orderedNames);
        });
    }

    [ConditionalTheory]
    [ConditionalRunTestOnlyIfHkcuRegistryAvailable]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void DeleteElements(bool delete1, bool delete2)
    {
        WithUniqueTempRegKey(regKey =>
        {
            var repository = new RegistryXmlRepository(regKey, NullLoggerFactory.Instance);

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
        });
    }

    // It would be nice to have a test paralleling the one in FileSystemXmlRepositoryTests.cs,
    // but there's no obvious way to simulate a failure for only one of the values.  You can
    // lock a whole key, but not individual values, and we don't have a hook to let us lock the
    // whole key while a particular value deletion is attempted.
    //[ConditionalFact]
    //[ConditionalConditionalRunTestOnlyIfHkcuRegistryAvailable]
    //public void DeleteElementsWithFailure()

    [ConditionalFact]
    [ConditionalRunTestOnlyIfHkcuRegistryAvailable]
    public void DeleteElementsWithOutOfBandDeletion()
    {
        WithUniqueTempRegKey(regKey =>
        {
            var repository = new RegistryXmlRepository(regKey, NullLoggerFactory.Instance);

            repository.StoreElement(new XElement("element1"), friendlyName: "friendly1");

            Assert.NotNull(regKey.GetValue("friendly1"));

            var ranSelector = false;

            Assert.True(repository.DeleteElements(deletableElements =>
            {
                ranSelector = true;

                // Now that the repository has read the element from the registry, delete it out-of-band.
                regKey.DeleteValue("friendly1");

                Assert.Single(deletableElements);

                deletableElements.First().DeletionOrder = 1;
            }));
            Assert.True(ranSelector);

            Assert.Null(regKey.GetValue("friendly1"));
        });
    }

    /// <summary>
    /// Runs a test and cleans up the registry key afterward.
    /// </summary>
    private static void WithUniqueTempRegKey(Action<RegistryKey> testCode)
    {
        string uniqueName = Guid.NewGuid().ToString();
        var uniqueSubkey = LazyHkcuTempKey.Value.CreateSubKey(uniqueName);
        try
        {
            testCode(uniqueSubkey);
        }
        finally
        {
            // clean up when test is done
            LazyHkcuTempKey.Value.DeleteSubKeyTree(uniqueName, throwOnMissingSubKey: false);
        }
    }

    private static readonly Lazy<RegistryKey> LazyHkcuTempKey = new Lazy<RegistryKey>(() =>
    {
        try
        {
            return Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\ASP.NET\temp");
        }
        catch
        {
            // swallow all failures
            return null;
        }
    });

    private class ConditionalRunTestOnlyIfHkcuRegistryAvailable : Attribute, ITestCondition
    {
        public bool IsMet => (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && LazyHkcuTempKey.Value != null);

        public string SkipReason { get; } = "HKCU registry couldn't be opened.";
    }
}
