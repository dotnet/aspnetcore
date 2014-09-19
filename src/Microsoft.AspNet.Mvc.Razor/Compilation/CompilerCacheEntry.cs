// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class CompilerCacheEntry
    {
        public CompilerCacheEntry([NotNull] RazorFileInfo info, [NotNull] Type viewType)
        {
            ViewType = viewType;
            RelativePath = info.RelativePath;
            Length = info.Length;
            LastModified = info.LastModified;
            Hash = info.Hash;
        }

        public CompilerCacheEntry([NotNull] RelativeFileInfo info, [NotNull] Type viewType)
        {
            ViewType = viewType;
            RelativePath = info.RelativePath;
            Length = info.FileInfo.Length;
            LastModified = info.FileInfo.LastModified;
        }

        public Type ViewType { get; set; }
        public string RelativePath { get; set; }
        public long Length { get; set; }
        public DateTime LastModified { get; set; }

        /// <summary>
        /// The file hash, should only be available for pre compiled files.
        /// </summary>
        public string Hash { get; set; }

        public bool IsPreCompiled {  get { return Hash != null; } }
    }
}
