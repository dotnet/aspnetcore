// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Internal;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class HostDocument : IEquatable<HostDocument>
    {
        public HostDocument(string filePath, string targetPath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (targetPath == null)
            {
                throw new ArgumentNullException(nameof(targetPath));
            }

            FilePath = filePath;
            TargetPath = targetPath;
        }

        public string FilePath { get; }

        public string TargetPath { get; }

        public override bool Equals(object obj)
        {
            return base.Equals(obj as DocumentSnapshot);
        }

        public bool Equals(DocumentSnapshot other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            return
                FilePathComparer.Instance.Equals(FilePath, other.FilePath) &&
                FilePathComparer.Instance.Equals(TargetPath, other.TargetPath);
        }

        public bool Equals(HostDocument other)
        {
            throw new NotImplementedException();
        }

        public override int GetHashCode()
        {
            var hash = new HashCodeCombiner();
            hash.Add(FilePath, FilePathComparer.Instance);
            hash.Add(TargetPath, FilePathComparer.Instance);
            return hash;
        }
    }
}
