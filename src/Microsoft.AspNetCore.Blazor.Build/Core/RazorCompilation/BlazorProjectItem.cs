// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using System.IO;

namespace Microsoft.AspNetCore.Blazor.Build.Core.RazorCompilation
{
    internal class BlazorProjectItem : RazorProjectItem
    {
        private readonly string _projectBasePath;
        private readonly string _itemFullPhysicalPath;
        private readonly Stream _itemContents;

        public BlazorProjectItem(
            string projectBasePath,
            string itemFullPhysicalPath,
            Stream itemFileContents)
        {
            _projectBasePath = projectBasePath;
            _itemFullPhysicalPath = itemFullPhysicalPath;
            _itemContents = itemFileContents;
        }

        public override string BasePath => _projectBasePath;

        public override string FilePath => _itemFullPhysicalPath;

        public override string PhysicalPath => _itemFullPhysicalPath;

        public override bool Exists => true;

        public override Stream Read() => _itemContents;
    }
}
