// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Win32;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection.Repositories
{
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
                Assert.Equal(0, allElements.Count);
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
                Guid parsedGuid = Guid.Parse(valueName as string);
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
}
