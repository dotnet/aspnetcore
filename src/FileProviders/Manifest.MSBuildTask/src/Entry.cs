// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Extensions.FileProviders.Embedded.Manifest.Task
{
    /// <summary>
    /// This API supports infrastructure and is not intended to be used
    /// directly from your code. This API may change or be removed in future releases.
    /// </summary>
    [DebuggerDisplay("{Name,nq}")]
    public class Entry : IEquatable<Entry>
    {
        public bool IsFile { get; private set; }

        public string Name { get; private set; }

        public string AssemblyResourceName { get; private set; }

        public ISet<Entry> Children { get; } = new SortedSet<Entry>(NameComparer.Instance);

        public static Entry Directory(string name) =>
            new Entry { Name = name };

        public static Entry File(string name, string assemblyResourceName) =>
            new Entry { Name = name, AssemblyResourceName = assemblyResourceName, IsFile = true };

        internal void AddChild(Entry child)
        {
            if (IsFile)
            {
                throw new InvalidOperationException("Tried to add children to a file.");
            }

            if (Children.Contains(child))
            {
                throw new InvalidOperationException($"An item with the name '{child.Name}' already exists.");
            }

            Children.Add(child);
        }

        internal Entry GetDirectory(string currentSegment)
        {
            if (IsFile)
            {
                throw new InvalidOperationException("Tried to get a directory from a file.");
            }

            foreach (var child in Children)
            {
                if (child.HasName(currentSegment))
                {
                    if (child.IsFile)
                    {
                        throw new InvalidOperationException("Tried to find a directory but found a file instead");
                    }
                    else
                    {
                        return child;
                    }
                }
            }

            return null;
        }

        public bool Equals(Entry other)
        {
            if (other == null || !other.HasName(Name) || other.IsFile != IsFile)
            {
                return false;
            }

            if (IsFile)
            {
                return string.Equals(other.AssemblyResourceName, AssemblyResourceName, StringComparison.Ordinal);
            }
            else
            {
                return SameChildren(Children, other.Children);
            }
        }

        private bool HasName(string currentSegment)
        {
            return string.Equals(Name, currentSegment, StringComparison.Ordinal);
        }

        private bool SameChildren(ISet<Entry> left, ISet<Entry> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            var le = left.GetEnumerator();
            var re = right.GetEnumerator();
            while (le.MoveNext() && re.MoveNext())
            {
                if (!le.Current.Equals(re.Current))
                {
                    return false;
                }
            }

            return true;
        }

        private class NameComparer : IComparer<Entry>
        {
            public static NameComparer Instance { get; } = new NameComparer();

            public int Compare(Entry x, Entry y) =>
                string.Compare(x?.Name, y?.Name, StringComparison.Ordinal);
        }
    }
}
