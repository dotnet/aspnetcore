// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.AspNet.DataProtection.Repositories
{
    /// <summary>
    /// An XML repository backed by a file system.
    /// </summary>
    public class FileSystemXmlRepository : IXmlRepository
    {
        public FileSystemXmlRepository([NotNull] DirectoryInfo directory)
        {
            Directory = directory;
        }

        protected DirectoryInfo Directory
        {
            get;
            private set;
        }

        public virtual IReadOnlyCollection<XElement> GetAllElements()
        {
            // forces complete enumeration
            return GetAllElementsImpl().ToArray();
        }

        private IEnumerable<XElement> GetAllElementsImpl()
        {
            Directory.Create(); // won't throw if the directory already exists

            // Find all files matching the pattern "{guid}.xml"
            foreach (var fileSystemInfo in Directory.EnumerateFileSystemInfos("*.xml", SearchOption.TopDirectoryOnly))
            {
                string simpleFilename = fileSystemInfo.Name;
                if (simpleFilename.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    simpleFilename = simpleFilename.Substring(0, simpleFilename.Length - ".xml".Length);
                }

                Guid unused;
                if (Guid.TryParseExact(simpleFilename, "D" /* registry format */, out unused))
                {
                    XDocument document;
                    using (var fileStream = File.OpenRead(fileSystemInfo.FullName))
                    {
                        document = XDocument.Load(fileStream);
                    }

                    // 'yield return' outside the preceding 'using' block so we don't hold files open longer than necessary
                    yield return document.Root;
                }
            }
        }

        public virtual void StoreElement([NotNull] XElement element, string friendlyName)
        {
            // We're going to ignore the friendly name for now and just use a GUID.
            StoreElement(element, Guid.NewGuid());
        }

        private void StoreElement(XElement element, Guid id)
        {
            // We're first going to write the file to a temporary location. This way, another consumer
            // won't try reading the file in the middle of us writing it. Additionally, if our process
            // crashes mid-write, we won't end up with a corrupt .xml file.

            Directory.Create(); // won't throw if the directory already exists
            string tempFilename = Path.Combine(Directory.FullName, String.Format(CultureInfo.InvariantCulture, "{0:D}.tmp", id));
            string finalFilename = Path.Combine(Directory.FullName, String.Format(CultureInfo.InvariantCulture, "{0:D}.xml", id));

            try
            {
                using (var tempFileStream = File.OpenWrite(tempFilename))
                {
                    new XDocument(element).Save(tempFileStream);
                }

                // Once the file has been fully written, perform the rename.
                // Renames are atomic operations on the file systems we support.
                File.Move(tempFilename, finalFilename);
            }
            finally
            {
                File.Delete(tempFilename); // won't throw if the file doesn't exist
            }
        }
    }
}
