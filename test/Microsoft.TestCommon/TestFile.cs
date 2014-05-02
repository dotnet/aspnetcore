// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System.IO;
using System.Reflection;
using Microsoft.TestCommon;

namespace System.Web.WebPages.TestUtils
{
    public class TestFile
    {
        public const string ResourceNameFormat = "{0}.TestFiles.{1}";

        public string ResourceName { get; set; }
        public Assembly Assembly { get; set; }

        public TestFile(string resName, Assembly asm)
        {
            ResourceName = resName;
            Assembly = asm;
        }

        public static TestFile Create(string localResourceName)
        {
            return new TestFile(String.Format(ResourceNameFormat, Assembly.GetCallingAssembly().GetName().Name, localResourceName), Assembly.GetCallingAssembly());
        }

        public Stream OpenRead()
        {
            Stream strm = Assembly.GetManifestResourceStream(ResourceName);
            if (strm == null)
            {
                Assert.True(false, String.Format("Manifest resource: {0} not found", ResourceName));
            }
            return strm;
        }

        public byte[] ReadAllBytes()
        {
            using (Stream stream = OpenRead())
            {
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                return buffer;
            }
        }

        public string ReadAllText()
        {
            using (StreamReader reader = new StreamReader(OpenRead()))
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

            using (Stream outStream = File.Create(filePath))
            {
                using (Stream inStream = OpenRead())
                {
                    inStream.CopyTo(outStream);
                }
            }
        }
    }
}
