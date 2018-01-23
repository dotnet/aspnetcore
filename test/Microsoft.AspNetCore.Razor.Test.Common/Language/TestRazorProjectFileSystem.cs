// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class TestRazorProjectFileSystem : RazorProjectFileSystem
    {
        public static RazorProjectFileSystem Empty = new TestRazorProjectFileSystem();

        private readonly Dictionary<string, RazorProjectItem> _lookup;

        public TestRazorProjectFileSystem()
            : this(new RazorProjectItem[0])
        {
        }

        public TestRazorProjectFileSystem(IList<RazorProjectItem> items)
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
