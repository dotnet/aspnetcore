// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRazorDirectiveFeature : IRazorDirectiveFeature, IRazorParserOptionsFeature
    {
        public ICollection<DirectiveDescriptor> Directives { get; } = new List<DirectiveDescriptor>();

        public RazorEngine Engine { get; set; }

        public int Order => 100;

        void IRazorParserOptionsFeature.Configure(RazorParserOptions options)
        {
            options.Directives.Clear();

            foreach (var directive in Directives)
            {
                options.Directives.Add(directive);
            }
        }
    }
}
