// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RepoTools.BuildGraph
{
    [DebuggerDisplay("{Name}")]
    public class Repository : IEquatable<Repository>
    {
        public Repository(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }

        public string RootDir { get; set; }

        public IList<Project> Projects { get; } = new List<Project>();

        public IList<Project> SupportProjects { get; } = new List<Project>();

        public IEnumerable<Project> AllProjects => Projects.Concat(SupportProjects);

        public bool Equals(Repository other) => string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Name);
    }
}
