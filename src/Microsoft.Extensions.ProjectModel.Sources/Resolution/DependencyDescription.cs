// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Extensions.ProjectModel.Resolution
{
    public class DependencyDescription
    {
        private readonly List<Dependency> _dependencies;

        public DependencyDescription(string name, string version, string path, string framework, string type, bool isResolved)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (string.IsNullOrEmpty(framework))
            {
                throw new ArgumentNullException(nameof(framework));
            }

            Name = name;
            Version = version;
            TargetFramework = framework;
            Resolved = isResolved;
            Path = path;
            DependencyType dt;
            Type = Enum.TryParse(type, ignoreCase: true , result: out dt) ? dt : DependencyType.Unknown;

            _dependencies = new List<Dependency>();
        }

        public string TargetFramework { get; }
        public string Name { get; }
        public string Path { get; }
        public string Version { get; }
        public DependencyType Type { get; }
        public bool Resolved { get; }
        public IEnumerable<Dependency> Dependencies => _dependencies;

        public void AddDependency(Dependency dependency)
        {
            _dependencies.Add(dependency);
        }
    }
}
