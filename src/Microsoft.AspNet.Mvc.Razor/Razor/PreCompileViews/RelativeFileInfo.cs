// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.FileSystems;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class RelativeFileInfo
    {
        public IFileInfo FileInfo { get; set; }
        public string RelativePath { get; set; }
    }
}