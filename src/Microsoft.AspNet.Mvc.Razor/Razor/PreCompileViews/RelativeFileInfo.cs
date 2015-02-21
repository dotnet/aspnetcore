// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.FileProviders;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// A container type that represents <see cref="IFileInfo"/> along with the application base relative path
    /// for a file in the file system.
    /// </summary>
    public class RelativeFileInfo
    {
        /// <summary>
        /// Initializes a new instance of <see cref="RelativeFileInfo"/>.
        /// </summary>
        /// <param name="fileInfo"><see cref="IFileInfo"/> for the file.</param>
        /// <param name="relativePath">Path of the file relative to the application base.</param>
        public RelativeFileInfo([NotNull] IFileInfo fileInfo, string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(relativePath));
            }

            FileInfo = fileInfo;
            RelativePath = relativePath;
        }

        /// <summary>
        /// Gets the <see cref="IFileInfo"/> associated with this instance of <see cref="RelativeFileInfo"/>.
        /// </summary>
        public IFileInfo FileInfo { get; }

        /// <summary>
        /// Gets the path of the file relative to the application base.
        /// </summary>
        public string RelativePath { get; }
    }
}