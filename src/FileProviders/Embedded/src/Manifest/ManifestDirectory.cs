// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.FileProviders.Embedded.Manifest
{
    internal class ManifestDirectory : ManifestEntry
    {
        protected ManifestDirectory(string name, ManifestEntry[] children)
            : base(name)
        {
            if (children == null)
            {
                throw new ArgumentNullException(nameof(children));
            }

            Children = children;
        }

        public IReadOnlyList<ManifestEntry> Children { get; protected set; }

        public override ManifestEntry Traverse(StringSegment segment)
        {
            if (segment.Equals(".", StringComparison.Ordinal))
            {
                return this;
            }

            if (segment.Equals("..", StringComparison.Ordinal))
            {
                return Parent;
            }

            foreach (var child in Children)
            {
                if (segment.Equals(child.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }

            return UnknownPath;
        }

        public virtual ManifestDirectory ToRootDirectory() => CreateRootDirectory(CopyChildren());

        public static ManifestDirectory CreateDirectory(string name, ManifestEntry[] children)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"'{nameof(name)}' must not be null, empty or whitespace.", nameof(name));
            }

            if (children == null)
            {
                throw new ArgumentNullException(nameof(children));
            }

            var result = new ManifestDirectory(name, children);
            ValidateChildrenAndSetParent(children, result);

            return result;
        }

        public static ManifestRootDirectory CreateRootDirectory(ManifestEntry[] children)
        {
            if (children == null)
            {
                throw new ArgumentNullException(nameof(children));
            }

            var result = new ManifestRootDirectory(children);
            ValidateChildrenAndSetParent(children, result);

            return result;
        }

        internal static void ValidateChildrenAndSetParent(ManifestEntry[] children, ManifestDirectory parent)
        {
            foreach (var child in children)
            {
                if (child == UnknownPath)
                {
                    throw new InvalidOperationException($"Invalid entry type '{nameof(ManifestSinkDirectory)}'");
                }

                if (child is ManifestRootDirectory)
                {
                    throw new InvalidOperationException($"Can't add a root folder as a child");
                }

                child.SetParent(parent);
            }
        }

        private ManifestEntry[] CopyChildren()
        {
            var list = new List<ManifestEntry>();
            for (int i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                switch (child)
                {
                    case ManifestSinkDirectory s:
                    case ManifestRootDirectory r:
                        throw new InvalidOperationException("Unexpected manifest node.");
                    case ManifestDirectory d:
                        var grandChildren = d.CopyChildren();
                        var newDirectory = CreateDirectory(d.Name, grandChildren);
                        list.Add(newDirectory);
                        break;
                    case ManifestFile f:
                        var file = new ManifestFile(f.Name, f.ResourcePath);
                        list.Add(file);
                        break;
                    default:
                        throw new InvalidOperationException("Unexpected manifest node.");
                }
            }

            return list.ToArray();
        }
    }
}
