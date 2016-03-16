// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Internal;

namespace Microsoft.Extensions.DependencyModel
{
    internal struct Dependency
    {
        public Dependency(string name, string version)
        {
            Name = name;
            Version = version;
        }

        public string Name { get; }
        public string Version { get; }

        public bool Equals(Dependency other)
        {
            return string.Equals(Name, other.Name) && string.Equals(Version, other.Version);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Dependency && Equals((Dependency) obj);
        }

        public override int GetHashCode()
        {
            var combiner = HashCodeCombiner.Start();
            combiner.Add(Name);
            combiner.Add(Version);
            return combiner.CombinedHash;
        }
    }
}