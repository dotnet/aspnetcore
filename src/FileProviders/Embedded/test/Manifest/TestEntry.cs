// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.Extensions.FileProviders.Embedded.Manifest
{
    class TestEntry
    {
        public bool IsFile => ResourcePath != null;
        public string Name { get; set; }
        public TestEntry[] Children { get; set; }
        public string ResourcePath { get; set; }

        public static TestEntry Directory(string name, params TestEntry[] entries) =>
            new TestEntry() { Name = name, Children = entries };

        public static TestEntry File(string name, string path = null) =>
            new TestEntry() { Name = name, ResourcePath = path ?? name };

        public XElement ToXElement() => IsFile ?
            new XElement("File", new XAttribute("Name", Name), new XElement("ResourcePath", ResourcePath)) :
            new XElement("Directory", new XAttribute("Name", Name), Children.Select(c => c.ToXElement()));

        public IEnumerable<TestEntry> GetFiles()
        {
            if (IsFile)
            {
                return Enumerable.Empty<TestEntry>();
            }

            var files = Children.Where(c => c.IsFile).ToArray();
            var otherFiles = Children.Where(c => !c.IsFile).SelectMany(d => d.GetFiles()).ToArray();

            return files.Concat(otherFiles).ToArray();
        }

    }
}
