using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RepoTools.BuildGraph
{
    [DebuggerDisplay("{Name}")]
    public class Project
    {
        public Project(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public string Path { get; set; }

        public Repository Repository { get; set; }

        public ISet<string> PackageReferences { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}
