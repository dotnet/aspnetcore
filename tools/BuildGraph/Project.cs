using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BuildGraph
{
    [DebuggerDisplay("{Name}")]
    public class Project
    {
        public Project(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public Repository Repository { get; set; }

        public IList<string> PackageReferences { get; set; } = Array.Empty<string>();
    }
}