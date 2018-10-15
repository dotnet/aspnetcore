// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.DataProtection.Repositories
{
    /// <summary>
    /// An XML repository backed by a file system.
    /// </summary>
    public class FileSystemXmlRepository : IXmlRepository
    {
        private static readonly Lazy<DirectoryInfo> _defaultDirectoryLazy = new Lazy<DirectoryInfo>(GetDefaultKeyStorageDirectory);

        private readonly ILogger _logger;

        /// <summary>
        /// Creates a <see cref="FileSystemXmlRepository"/> with keys stored at the given directory.
        /// </summary>
        /// <param name="directory">The directory in which to persist key material.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public FileSystemXmlRepository(DirectoryInfo directory, ILoggerFactory loggerFactory)
        {
            if (directory == null)
            {
                throw new ArgumentNullException(nameof(directory));
            }

            Directory = directory;
            _logger = loggerFactory.CreateLogger<FileSystemXmlRepository>();

            try
            {
                if (DockerUtils.IsDocker && !DockerUtils.IsVolumeMountedFolder(Directory))
                {
                    // warn users that keys may be lost when running in docker without a volume mounted folder
                    _logger.UsingEphemeralFileSystemLocationInContainer(Directory.FullName);
                }
            }
            catch (Exception ex)
            {
                // Treat exceptions as non-fatal when attempting to detect docker. 
                // These might occur if fstab is an unrecognized format, or if there are other unusual
                // file IO errors.
                _logger.LogTrace(ex, "Failure occurred while attempting to detect docker.");
            }
        }

        /// <summary>
        /// The default key storage directory.
        /// On Windows, this currently corresponds to "Environment.SpecialFolder.LocalApplication/ASP.NET/DataProtection-Keys".
        /// On Linux and macOS, this currently corresponds to "$HOME/.aspnet/DataProtection-Keys".
        /// </summary>
        /// <remarks>
        /// This property can return null if no suitable default key storage directory can
        /// be found, such as the case when the user profile is unavailable.
        /// </remarks>
        public static DirectoryInfo DefaultKeyStorageDirectory => _defaultDirectoryLazy.Value;

        /// <summary>
        /// The directory into which key material will be written.
        /// </summary>
        public DirectoryInfo Directory { get; }

        private const string DataProtectionKeysFolderName = "DataProtection-Keys";

        private static DirectoryInfo GetKeyStorageDirectoryFromBaseAppDataPath(string basePath)
        {
            return new DirectoryInfo(Path.Combine(basePath, "ASP.NET", DataProtectionKeysFolderName));
        }

        public virtual IReadOnlyCollection<XElement> GetAllElements()
        {
            // forces complete enumeration
            return GetAllElementsCore().ToList().AsReadOnly();
        }

        private IEnumerable<XElement> GetAllElementsCore()
        {
            Directory.Create(); // won't throw if the directory already exists

            // Find all files matching the pattern "*.xml".
            // Note: Inability to read any file is considered a fatal error (since the file may contain
            // revocation information), and we'll fail the entire operation rather than return a partial
            // set of elements. If a file contains well-formed XML but its contents are meaningless, we
            // won't fail that operation here. The caller is responsible for failing as appropriate given
            // that scenario.
            foreach (var fileSystemInfo in Directory.EnumerateFileSystemInfos("*.xml", SearchOption.TopDirectoryOnly))
            {
                yield return ReadElementFromFile(fileSystemInfo.FullName);
            }
        }

        private static DirectoryInfo GetDefaultKeyStorageDirectory()
        {
            DirectoryInfo retVal;

            // Environment.GetFolderPath returns null if the user profile isn't loaded.
            var localAppDataFromSystemPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var localAppDataFromEnvPath = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            var userProfilePath = Environment.GetEnvironmentVariable("USERPROFILE");
            var homePath = Environment.GetEnvironmentVariable("HOME");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !string.IsNullOrEmpty(localAppDataFromSystemPath))
            {
                // To preserve backwards-compatibility with 1.x, Environment.SpecialFolder.LocalApplicationData
                // cannot take precedence over $LOCALAPPDATA and $HOME/.aspnet on non-Windows platforms
                retVal = GetKeyStorageDirectoryFromBaseAppDataPath(localAppDataFromSystemPath);
            }
            else if (localAppDataFromEnvPath != null)
            {
                retVal = GetKeyStorageDirectoryFromBaseAppDataPath(localAppDataFromEnvPath);
            }
            else if (userProfilePath != null)
            {
                retVal = GetKeyStorageDirectoryFromBaseAppDataPath(Path.Combine(userProfilePath, "AppData", "Local"));
            }
            else if (homePath != null)
            {
                // If LOCALAPPDATA and USERPROFILE are not present but HOME is,
                // it's a good guess that this is a *NIX machine.  Use *NIX conventions for a folder name.
                retVal = new DirectoryInfo(Path.Combine(homePath, ".aspnet", DataProtectionKeysFolderName));
            }
            else if (!string.IsNullOrEmpty(localAppDataFromSystemPath))
            {
                // Starting in 2.x, non-Windows platforms may use Environment.SpecialFolder.LocalApplicationData
                // but only after checking for $LOCALAPPDATA, $USERPROFILE, and $HOME.
                retVal = GetKeyStorageDirectoryFromBaseAppDataPath(localAppDataFromSystemPath);
            }
            else
            {
                return null;
            }

            Debug.Assert(retVal != null);

            try
            {
                retVal.Create(); // throws if we don't have access, e.g., user profile not loaded
                return retVal;
            }
            catch
            {
                return null;
            }
        }

        internal static DirectoryInfo GetKeyStorageDirectoryForAzureWebSites()
        {
            // Azure Web Sites needs to be treated specially, as we need to store the keys in a
            // correct persisted location. We use the existence of the %WEBSITE_INSTANCE_ID% env
            // variable to determine if we're running in this environment, and if so we then use
            // the %HOME% variable to build up our base key storage path.
            if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID")))
            {
                var homeEnvVar = Environment.GetEnvironmentVariable("HOME");
                if (!String.IsNullOrEmpty(homeEnvVar))
                {
                    return GetKeyStorageDirectoryFromBaseAppDataPath(homeEnvVar);
                }
            }

            // nope
            return null;
        }

        private static bool IsSafeFilename(string filename)
        {
            // Must be non-empty and contain only a-zA-Z0-9, hyphen, and underscore.
            return (!String.IsNullOrEmpty(filename) && filename.All(c =>
                c == '-'
                || c == '_'
                || ('0' <= c && c <= '9')
                || ('A' <= c && c <= 'Z')
                || ('a' <= c && c <= 'z')));
        }

        private XElement ReadElementFromFile(string fullPath)
        {
            _logger.ReadingDataFromFile(fullPath);

            using (var fileStream = File.OpenRead(fullPath))
            {
                return XElement.Load(fileStream);
            }
        }

        public virtual void StoreElement(XElement element, string friendlyName)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            if (!IsSafeFilename(friendlyName))
            {
                var newFriendlyName = Guid.NewGuid().ToString();
                _logger.NameIsNotSafeFileName(friendlyName, newFriendlyName);
                friendlyName = newFriendlyName;
            }

            StoreElementCore(element, friendlyName);
        }

        private void StoreElementCore(XElement element, string filename)
        {
            // We're first going to write the file to a temporary location. This way, another consumer
            // won't try reading the file in the middle of us writing it. Additionally, if our process
            // crashes mid-write, we won't end up with a corrupt .xml file.

            Directory.Create(); // won't throw if the directory already exists
            var tempFilename = Path.Combine(Directory.FullName, Guid.NewGuid().ToString() + ".tmp");
            var finalFilename = Path.Combine(Directory.FullName, filename + ".xml");

            try
            {
                using (var tempFileStream = File.OpenWrite(tempFilename))
                {
                    element.Save(tempFileStream);
                }

                // Once the file has been fully written, perform the rename.
                // Renames are atomic operations on the file systems we support.
                _logger.WritingDataToFile(finalFilename);

                // Use File.Copy because File.Move on NFS shares has issues in .NET Core 2.0
                File.Copy(tempFilename, finalFilename);
            }
            finally
            {
                File.Delete(tempFilename); // won't throw if the file doesn't exist
            }
        }
    }
}
