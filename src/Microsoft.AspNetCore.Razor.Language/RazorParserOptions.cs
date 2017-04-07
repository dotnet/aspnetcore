// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Razor.Language
{
    public sealed class RazorParserOptions
    {
        public static RazorParserOptions CreateDefaultOptions()
        {
            return new RazorParserOptions();
        }

        private RazorParserOptions()
        {
            Directives = new List<DirectiveDescriptor>();
            NamespaceImports = new HashSet<string>(StringComparer.Ordinal) { nameof(System), typeof(Task).Namespace };
        }

        public bool DesignTimeMode { get; set; }

        public int TabSize { get; set; } = 4;

        public bool IsIndentingWithTabs { get; set; }

        public ICollection<DirectiveDescriptor> Directives { get; }

        public HashSet<string> NamespaceImports { get; }
    }
}
