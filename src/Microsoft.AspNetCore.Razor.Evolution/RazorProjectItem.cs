// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.IO;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    /// <summary>
    /// An item in <see cref="RazorProject"/>.
    /// </summary>
    [DebuggerDisplay("{CombinedPath}")]
    public abstract class RazorProjectItem
    {
        /// <summary>
        /// Path specified in <see cref="RazorProject.EnumerateItems(string)"/>.
        /// </summary>
        public abstract string BasePath { get; }

        /// <summary>
        /// Path relative to <see cref="BasePath"/>.
        /// </summary>
        public abstract string Path { get; }

        /// <summary>
        /// The absolute path to the file, including the file name.
        /// </summary>
        public abstract string PhysicalPath { get; }

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
        public virtual string CombinedPath
        {
            get
            {
                if (BasePath == "/")
                {
                    return Path;
                }
                else
                {
                    return BasePath + Path;
                }
            }
        }

        /// <summary>
        /// The extension of the file.
        /// </summary>
        public virtual string Extension
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
        public virtual string FileName
        {
            get
            {
                var index = Path.LastIndexOf('/');
                return Path.Substring(index + 1);
            }
        }

        /// <summary>
        /// Path relative to <see cref="BasePath"/> without the extension.
        /// </summary>
        public virtual string PathWithoutExtension
        {
            get
            {
                var index = Path.LastIndexOf('.');
                if (index == -1)
                {
                    return Path;
                }
                else
                {
                    return Path.Substring(0, index);
                }
            }
        }
    }
}