// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class HostDocument
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
            GeneratedCodeContainer = new GeneratedCodeContainer();
        }

        public string FilePath { get; }

        public string TargetPath { get; }

        public GeneratedCodeContainer GeneratedCodeContainer { get; }
    }
}
