// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.FileProviders
{
    internal class FileInfoComparer : IEqualityComparer<IFileInfo>
    {
        public static FileInfoComparer Instance { get; set; } = new FileInfoComparer();

        public bool Equals(IFileInfo x, IFileInfo y)
        {
            if (x == null && y == null)
            {
                return true;
            }

            if ((x == null && y != null) || (x != null && y == null))
            {
                return false;
            }

            return x.Exists == y.Exists &&
                x.IsDirectory == y.IsDirectory &&
                x.Length == y.Length &&
                string.Equals(x.Name, y.Name, StringComparison.Ordinal) &&
                string.Equals(x.PhysicalPath, y.PhysicalPath, StringComparison.Ordinal);
        }

        public int GetHashCode(IFileInfo obj) => 0;
    }
}
