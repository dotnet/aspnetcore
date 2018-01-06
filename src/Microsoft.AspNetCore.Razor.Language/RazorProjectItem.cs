// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;

namespace Microsoft.AspNetCore.Razor.Language
{
    /// <summary>
    /// An item in <see cref="RazorProject"/>.
    /// </summary>
    [DebuggerDisplay("{" + nameof(DebuggerToString) + "()}")]
    public abstract class RazorProjectItem
    {
        /// <summary>
        /// Path specified in <see cref="RazorProject.EnumerateItems(string)"/>.
        /// </summary>
        public abstract string BasePath { get; }

        /// <summary>
        /// File path relative to <see cref="BasePath"/>. This property uses the project path syntax,
        /// using <c>/</c> as a path separator and does not follow the operating system's file system
        /// conventions.
        /// </summary>
        public abstract string FilePath { get; }

        /// <summary>
        /// The absolute physical (file system) path to the file, including the file name.
        /// </summary>
        public abstract string PhysicalPath { get; }

        /// <summary>
        /// The relative physical (file system) path to the file, including the file name. Relative to the
        /// physical path of the <see cref="BasePath"/>.
        /// </summary>
        public virtual string RelativePhysicalPath => null;

        /// <summary>
        /// Gets the file contents as readonly <see cref="Stream"/>.
        /// </summary>
        /// <returns>The <see cref="Stream"/>.</returns>
        public abstract Stream Read();

        /// <summary>
        /// Gets a value that determines if the file exists.
        /// </summary>
        public abstract bool Exists { get; }

        /// <summary>
        /// The root relative path of the item.
        /// </summary>
        public string CombinedPath
        {
            get
            {
                if (BasePath == "/")
                {
                    return FilePath;
                }
                else
                {
                    return BasePath + FilePath;
                }
            }
        }

        /// <summary>
        /// The extension of the file.
        /// </summary>
        public string Extension
        {
            get
            {
                var index = FileName.LastIndexOf('.');
                if (index == -1)
                {
                    return null;
                }
                else
                {
                    return FileName.Substring(index);
                }
            }
        }

        /// <summary>
        /// The name of the file including the extension.
        /// </summary>
        public string FileName
        {
            get
            {
                var index = FilePath.LastIndexOf('/');
                return FilePath.Substring(index + 1);
            }
        }

        /// <summary>
        /// File path relative to <see cref="BasePath"/> without the extension.
        /// </summary>
        public string FilePathWithoutExtension
        {
            get
            {
                var index = FilePath.LastIndexOf('.');
                if (index == -1)
                {
                    return FilePath;
                }
                else
                {
                    return FilePath.Substring(0, index);
                }
            }
        }

        private string DebuggerToString()
        {
            return CombinedPath;
        }
    }
}