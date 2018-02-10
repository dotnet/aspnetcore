// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class TestRazorProjectFileSystem : DefaultRazorProjectFileSystem
    {
        public static RazorProjectFileSystem Empty = new TestRazorProjectFileSystem();

        private readonly Dictionary<string, RazorProjectItem> _lookup;

        public TestRazorProjectFileSystem()
            : this(new RazorProjectItem[0])
        {
        }

        public TestRazorProjectFileSystem(IList<RazorProjectItem> items) : base("/")
        {
            _lookup = items.ToDictionary(item => item.FilePath);
        }

        public override IEnumerable<RazorProjectItem> EnumerateItems(string basePath)
        {
            throw new NotImplementedException();
        }

        public override RazorProjectItem GetItem(string path)
        {
            if (!_lookup.TryGetValue(path, out var value))
            {
                value = new NotFoundProjectItem("", path);
            }

            return value;
        }
    }
}
