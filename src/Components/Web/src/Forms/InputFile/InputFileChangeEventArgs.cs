// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// Supplies information about an <see cref="InputFile.OnChange"/> event being raised.
    /// </summary>
    public class InputFileChangeEventArgs : EventArgs
    {
        private readonly IReadOnlyList<IBrowserFile> _files;
        private readonly long _maxFileSize;
        private readonly int _maxAllowedFiles;

        private bool _validated;

        /// <summary>
        /// Gets the updated file entries list.
        /// </summary>
        public IReadOnlyList<IBrowserFile> Files
        {
            get
            {
                ValidateFiles();
                return _files;
            }
        }

        /// <summary>
        /// Constructs a new <see cref="InputFileChangeEventArgs"/> instance.
        /// </summary>
        /// <param name="files">The updated file entries list.</param>
        /// <param name="maxFileSize">The maximum allowed file size in bytes.</param>
        /// <param name="maxAllowedFiles">The maximum allowed number of files.</param>
        public InputFileChangeEventArgs(IReadOnlyList<IBrowserFile> files, long maxFileSize, int maxAllowedFiles)
        {
            _files = files;
            _maxFileSize = maxFileSize;
            _maxAllowedFiles = maxAllowedFiles;
        }

        private void ValidateFiles()
        {
            if (_validated)
            {
                return;
            }

            if (_files.Count > _maxAllowedFiles)
            {
                throw new InvalidOperationException($"Expected a maximum of {_maxAllowedFiles} files, but got {_files.Count}.");
            }

            foreach (var file in _files)
            {
                if (file.Size < 0)
                {
                    throw new InvalidOperationException("Files cannot have a negative file size.");
                }

                if (file.Size > _maxFileSize)
                {
                    throw new InvalidOperationException($"File with size {file.Size} bytes exceeded the maximum of {_maxFileSize} bytes.");
                }
            }

            _validated = true;
        }
    }
}
