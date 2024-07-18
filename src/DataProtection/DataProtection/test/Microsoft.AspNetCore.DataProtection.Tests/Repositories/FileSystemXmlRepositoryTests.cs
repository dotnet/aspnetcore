// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.DataProtection.Repositories;

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
            Guid parsedGuid = Guid.Parse(filenameNoSuffix, CultureInfo.InvariantCulture);
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

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void DeleteElements(bool delete1, bool delete2)
    {
        WithUniqueTempDirectory(dirInfo =>
        {
            var repository = new FileSystemXmlRepository(dirInfo, NullLoggerFactory.Instance);

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

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux, SkipReason = "Making FileSystemInfo.Delete throw on Linux is hard")]
    [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Making FileSystemInfo.Delete throw on macOS is hard")]
    public void DeleteElementsWithFailure()
    {
        WithUniqueTempDirectory(dirInfo =>
        {
            var repository = new FileSystemXmlRepository(dirInfo, NullLoggerFactory.Instance);

            repository.StoreElement(new XElement("element1"), friendlyName: "friendly1");
            repository.StoreElement(new XElement("element2"), friendlyName: "friendly2");
            repository.StoreElement(new XElement("element3"), friendlyName: "friendly3");

            var filePath1 = Path.Combine(dirInfo.FullName, "friendly1.xml");
            var filePath2 = Path.Combine(dirInfo.FullName, "friendly2.xml");
            var filePath3 = Path.Combine(dirInfo.FullName, "friendly3.xml");

            Assert.True(File.Exists(filePath1));
            Assert.True(File.Exists(filePath2));
            Assert.True(File.Exists(filePath3));

            IDisposable fileLock2 = null;
            try
            {
                var ranSelector = false;
                Assert.False(repository.DeleteElements(deletableElements =>
                {
                    ranSelector = true;

                    // Now that the repository has read the files from disk, lock one to prevent deletion from succeeding
                    fileLock2 = new FileStream(Path.Combine(dirInfo.FullName, "friendly2.xml"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);

                    Assert.Equal(3, deletableElements.Count);

                    var i = 4;
                    foreach (var deletableElement in deletableElements)
                    {
                        // Delete in reverse alphabetical order, so the results aren't coincidental.
                        deletableElement.DeletionOrder = i--;
                    }
                }));
                Assert.True(ranSelector);
            }
            finally
            {
                fileLock2?.Dispose();
            }

            Assert.True(File.Exists(filePath1)); // Deletion not attempted after failure
            Assert.True(File.Exists(filePath2)); // Deletion fails because of lock
            Assert.False(File.Exists(filePath3)); // Deleted before error
        });
    }

    [Fact]
    public void DeleteElementsWithOutOfBandDeletion()
    {
        WithUniqueTempDirectory(dirInfo =>
        {
            var repository = new FileSystemXmlRepository(dirInfo, NullLoggerFactory.Instance);

            repository.StoreElement(new XElement("element1"), friendlyName: "friendly1");

            var filePath = Path.Combine(dirInfo.FullName, "friendly1.xml");
            Assert.True(File.Exists(filePath));

            var ranSelector = false;

            Assert.True(repository.DeleteElements(deletableElements =>
            {
                ranSelector = true;

                // Now that the repository has read the element from disk, delete it out-of-band.
                File.Delete(filePath);

                Assert.Equal(1, deletableElements.Count);

                deletableElements.First().DeletionOrder = 1;
            }));
            Assert.True(ranSelector);

            Assert.False(File.Exists(filePath));
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

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows, SkipReason = "UnixFileMode is not supported on Windows.")]
    public void StoreElement_CreatesFileWithUserOnlyUnixFileMode()
    {
        WithUniqueTempDirectory(dirInfo =>
        {
            // Arrange
            var element = XElement.Parse("<element1 />");
            var repository = new FileSystemXmlRepository(dirInfo, NullLoggerFactory.Instance);

            // Act
            repository.StoreElement(element, "friendly-name");

            // Assert
            var fileInfo = Assert.Single(dirInfo.GetFiles());
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, fileInfo.UnixFileMode);
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
