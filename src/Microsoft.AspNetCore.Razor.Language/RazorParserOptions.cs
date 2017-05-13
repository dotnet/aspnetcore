// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

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
        }

        public bool DesignTimeMode { get; set; }

        public int TabSize { get; set; } = 4;

        public bool IsIndentingWithTabs { get; set; }

        public bool StopParsingAfterFirstDirective { get; set; }

        public ICollection<DirectiveDescriptor> Directives { get; }
    }
}
