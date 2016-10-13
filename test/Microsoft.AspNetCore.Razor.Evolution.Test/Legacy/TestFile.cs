// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal class TestFile
    {
        public TestFile(string resourceName, Assembly assembly)
        {
            Assembly = assembly;
            ResourceName = Assembly.GetName().Name + "." + resourceName.Replace('/', '.');
        }

        public Assembly Assembly { get; }

        public string ResourceName { get; }

        public static TestFile Create(string localResourceName)
        {
            return new TestFile(localResourceName, typeof(TestFile).GetTypeInfo().Assembly);
        }

        public Stream OpenRead()
        {
            var stream = Assembly.GetManifestResourceStream(ResourceName);
            if (stream == null)
            {
                Assert.True(false, string.Format("Manifest resource: {0} not found", ResourceName));
            }

            return stream;
        }

        public bool Exists()
        {
            var resourceNames = Assembly.GetManifestResourceNames();
            foreach (var resourceName in resourceNames)
            {
                // Resource names are case-sensitive.
                if (string.Equals(ResourceName, resourceName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public byte[] ReadAllBytes()
        {
            using (var stream = OpenRead())
            {
                var buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);

                return buffer;
            }
        }

        public string ReadAllText()
        {
            using (var reader = new StreamReader(OpenRead()))
            {
                // The .Replace() calls normalize line endings, in case you get \n instead of \r\n
                // since all the unit tests rely on the assumption that the files will have \r\n endings.
                return reader.ReadToEnd().Replace("\r", "").Replace("\n", "\r\n");
            }
        }

        /// <summary>
        /// Saves the file to the specified path.
        /// </summary>
        public void Save(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var outStream = File.Create(filePath))
            {
                using (var inStream = OpenRead())
                {
                    inStream.CopyTo(outStream);
                }
            }
        }
    }
}
