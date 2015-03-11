// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.DataProtection.Repositories
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
        public FileSystemXmlRepository([NotNull] DirectoryInfo directory)
            : this(directory, services: null)
        {
        }

        /// <summary>
        /// Creates a <see cref="FileSystemXmlRepository"/> with keys stored at the given directory.
        /// </summary>
        /// <param name="directory">The directory in which to persist key material.</param>
        /// <param name="services">An optional <see cref="IServiceProvider"/> to provide ancillary services.</param>
        public FileSystemXmlRepository([NotNull] DirectoryInfo directory, IServiceProvider services)
        {
            Directory = directory;
            Services = services;
            _logger = services?.GetLogger<FileSystemXmlRepository>();
        }

        /// <summary>
        /// The default key storage directory, which currently corresponds to
        /// "%LOCALAPPDATA%\ASP.NET\DataProtection-Keys".
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

        /// <summary>
        /// The <see cref="IServiceProvider"/> provided to the constructor.
        /// </summary>
        protected IServiceProvider Services { get; }

        private static DirectoryInfo GetKeyStorageDirectoryFromBaseAppDataPath(string basePath)
        {
            return new DirectoryInfo(Path.Combine(basePath, "ASP.NET", "DataProtection-Keys"));
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
#if !DNXCORE50
            // Environment.GetFolderPath returns null if the user profile isn't loaded.
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!String.IsNullOrEmpty(folderPath))
            {
                return GetKeyStorageDirectoryFromBaseAppDataPath(folderPath);
            }
            else
            {
                return null;
            }
#else
            // On core CLR, we need to fall back to environment variables.
            string folderPath = Environment.GetEnvironmentVariable("LOCALAPPDATA")
                ?? Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE"), "AppData", "Local");

            DirectoryInfo retVal = GetKeyStorageDirectoryFromBaseAppDataPath(folderPath);
            try
            {
                retVal.Create(); // throws if we don't have access, e.g., user profile not loaded
                return retVal;
            }
            catch
            {
                return null;
            }
#endif
        }

        internal static DirectoryInfo GetKeyStorageDirectoryForAzureWebSites()
        {
            // Azure Web Sites needs to be treated specially, as we need to store the keys in a
            // correct persisted location. We use the existence of the %WEBSITE_INSTANCE_ID% env
            // variable to determine if we're running in this environment, and if so we then use
            // the %HOME% variable to build up our base key storage path.
            if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID")))
            {
                string homeEnvVar = Environment.GetEnvironmentVariable("HOME");
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
            if (_logger.IsVerboseLevelEnabled())
            {
                _logger.LogVerbose("Reading data from file '{0}'.", fullPath);
            }

            using (var fileStream = File.OpenRead(fullPath))
            {
                return XElement.Load(fileStream);
            }
        }

        public virtual void StoreElement([NotNull] XElement element, string friendlyName)
        {
            if (!IsSafeFilename(friendlyName))
            {
                string newFriendlyName = Guid.NewGuid().ToString();
                if (_logger.IsVerboseLevelEnabled())
                {
                    _logger.LogVerbose("The name '{0}' is not a safe file name, using '{1}' instead.", friendlyName, newFriendlyName);
                }
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
            string tempFilename = Path.Combine(Directory.FullName, Guid.NewGuid().ToString() + ".tmp");
            string finalFilename = Path.Combine(Directory.FullName, filename + ".xml");

            try
            {
                using (var tempFileStream = File.OpenWrite(tempFilename))
                {
                    element.Save(tempFileStream);
                }

                // Once the file has been fully written, perform the rename.
                // Renames are atomic operations on the file systems we support.
                if (_logger.IsInformationLevelEnabled())
                {
                    _logger.LogInformation("Writing data to file '{0}.", finalFilename);
                }
                File.Move(tempFilename, finalFilename);
            }
            finally
            {
                File.Delete(tempFilename); // won't throw if the file doesn't exist
            }
        }
    }
}
