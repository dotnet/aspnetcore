// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Editor.Razor
{
    internal class TextSnapshotProjectItem : RazorProjectItem
    {
        private readonly ITextSnapshot _snapshot;

        public TextSnapshotProjectItem(ITextSnapshot snapshot, string projectDirectory, string relativeFilePath, string filePath)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (string.IsNullOrEmpty(projectDirectory))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(projectDirectory));
            }

            if (string.IsNullOrEmpty(relativeFilePath))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(relativeFilePath));
            }

            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(filePath));
            }

            _snapshot = snapshot;
            BasePath = projectDirectory;
            FilePath = relativeFilePath;
            PhysicalPath = filePath;
        }

        public override string BasePath { get; }

        public override string FilePath { get; }

        public override string PhysicalPath { get; }

        public override bool Exists => true;

        public override Stream Read()
        {
            var charArray = _snapshot.ToCharArray(0, _snapshot.Length);

            // We can assume UTF8 because the call path that reads from RazorProjectItem => SourceDocument
            // can't determine the encoding and always assumes Encoding.UTF8. This is something that we might
            // want to revisit in the future.
            var bytes = Encoding.UTF8.GetBytes(charArray);
            var memoryStream = new MemoryStream(bytes);
            return memoryStream;
        }
    }

}
