// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc.Razor.Precompilation
{
    public class RazorFileInfo
    {
        /// <summary>
        /// Type name including namespace.
        /// </summary>
        public string FullTypeName { get; set; }

        /// <summary>
        /// Last modified at compilation time.
        /// </summary>
        public DateTimeOffset LastModified { get; set; }

        /// <summary>
        /// The length of the file in bytes.
        /// </summary>
        public long Length { get; set; }

        /// <summary>
        /// Path to to the file relative to the application base.
        /// </summary>
        public string RelativePath { get; set; }

        /// <summary>
        /// A hash of the file content.
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// The version of hash algorithm used to generate <see cref="Hash"/>.
        /// </summary>
        public int HashAlgorithmVersion { get; set; }
    }
}