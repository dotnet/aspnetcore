// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection.Repositories
{
    public class FileSystemXmlRepositoryTests
    {
        [Fact]
        public void DefaultKeyStorageDirectory_Property()
        {
            var baseDir = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ASP.NET")
                : Path.Combine(Environment.GetEnvironmentVariable("HOME"), ".aspnet");
            var expectedDir = new DirectoryInfo(Path.Combine(baseDir, "DataProtection-Keys")).FullName;

            // Act
            var defaultDirInfo = FileSystemXmlRepository.DefaultKeyStorageDirectory;

            // Assert
            Assert.Equal(expectedDir, defaultDirInfo.FullName);
        }

        [Fact]
        public void Directory_Property()
        {
            WithUniqueTempDirectory(dirInfo =>
            {
                // Arrange
                var repository = new FileSystemXmlRepository(dirInfo, NullLoggerFactory.Instance);

                // Act
                var retVal = repository.Directory;

                // Assert
                Assert.Equal(dirInfo, retVal);
            });
        }

        [Fact]
        public void GetAllElements_EmptyOrNonexistentDirectory_ReturnsEmptyCollection()
        {
            WithUniqueTempDirectory(dirInfo =>
            {
                // Arrange
                var repository = new FileSystemXmlRepository(dirInfo, NullLoggerFactory.Instance);

                // Act
                var allElements = repository.GetAllElements();

                // Assert
                Assert.Equal(0, allElements.Count);
            });
        }

        [Fact]
        public void StoreElement_WithValidFriendlyName_UsesFriendlyName()
        {
            WithUniqueTempDirectory(dirInfo =>
            {
                // Arrange
                var element = XElement.Parse("<element1 />");
                var repository = new FileSystemXmlRepository(dirInfo, NullLoggerFactory.Instance);

                // Act
                repository.StoreElement(element, "valid-friendly-name");

                // Assert
                var fileInfos = dirInfo.GetFiles();
                var fileInfo = fileInfos.Single(); // only one file should've been created

                // filename should be "valid-friendly-name.xml"
                Assert.Equal("valid-friendly-name.xml", fileInfo.Name, StringComparer.OrdinalIgnoreCase);

                // file contents should be "<element1 />"
                var parsedElement = XElement.Parse(File.ReadAllText(fileInfo.FullName));
                XmlAssert.Equal("<element1 />", parsedElement);
            });
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("..")]
        [InlineData("not*friendly")]
        public void StoreElement_WithInvalidFriendlyName_CreatesNewGuidAsName(string friendlyName)
        {
            WithUniqueTempDirectory(dirInfo =>
            {
                // Arrange
                var element = XElement.Parse("<element1 />");
                var repository = new FileSystemXmlRepository(dirInfo, NullLoggerFactory.Instance);

                // Act
                repository.StoreElement(element, friendlyName);

                // Assert
                var fileInfos = dirInfo.GetFiles();
                var fileInfo = fileInfos.Single(); // only one file should've been created

                // filename should be "{GUID}.xml"
                var filename = fileInfo.Name;
                Assert.EndsWith(".xml", filename, StringComparison.OrdinalIgnoreCase);
                var filenameNoSuffix = filename.Substring(0, filename.Length - ".xml".Length);
                Guid parsedGuid = Guid.Parse(filenameNoSuffix);
                Assert.NotEqual(Guid.Empty, parsedGuid);

                // file contents should be "<element1 />"
                var parsedElement = XElement.Parse(File.ReadAllText(fileInfo.FullName));
                XmlAssert.Equal("<element1 />", parsedElement);
            });
        }

        [Fact]
        public void StoreElements_ThenRetrieve_SeesAllElements()
        {
            WithUniqueTempDirectory(dirInfo =>
            {
                // Arrange
                var repository = new FileSystemXmlRepository(dirInfo, NullLoggerFactory.Instance);

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

        [ConditionalFact]
        [DockerOnly]
        [Trait("Docker", "true")]
        public void Logs_DockerEphemeralFolders()
        {
            // Arrange
            var loggerFactory = new StringLoggerFactory(LogLevel.Warning);
            WithUniqueTempDirectory(dirInfo =>
            {
                // Act
                var repo = new FileSystemXmlRepository(dirInfo, loggerFactory);

                // Assert
                Assert.Contains(Resources.FormatFileSystem_EphemeralKeysLocationInContainer(dirInfo.FullName), loggerFactory.ToString());
            });
        }

        /// <summary>
        /// Runs a test and cleans up the temp directory afterward.
        /// </summary>
        private static void WithUniqueTempDirectory(Action<DirectoryInfo> testCode)
        {
            string uniqueTempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var dirInfo = new DirectoryInfo(uniqueTempPath);
            try
            {
                testCode(dirInfo);
            }
            finally
            {
                // clean up when test is done
                if (dirInfo.Exists)
                {
                    dirInfo.Delete(recursive: true);
                }
            }
        }
    }
}
