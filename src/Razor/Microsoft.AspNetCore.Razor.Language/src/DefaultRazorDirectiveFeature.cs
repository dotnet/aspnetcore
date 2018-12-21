// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRazorDirectiveFeature : RazorEngineFeatureBase, IRazorDirectiveFeature, IConfigureRazorParserOptionsFeature
    {
        public ICollection<DirectiveDescriptor> Directives { get; } = new List<DirectiveDescriptor>();

        public IDictionary<string, ICollection<DirectiveDescriptor>> DirectivesByFileKind { get; } = new Dictionary<string, ICollection<DirectiveDescriptor>>(StringComparer.OrdinalIgnoreCase);

        public int Order => 100;

        void IConfigureRazorParserOptionsFeature.Configure(RazorParserOptionsBuilder options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.Directives.Clear();

            foreach (var directive in Directives)
            {
                options.Directives.Add(directive);
            }

            if (options.FileKind != null && DirectivesByFileKind.TryGetValue(options.FileKind, out var directives))
            {
                foreach (var directive in directives)
                {
                    // Replace any non-file-kind-specific directives
                    var replaces = options.Directives.Where(d => string.Equals(d.Directive, directive.Directive, StringComparison.Ordinal)).ToArray();
                    foreach (var replace in replaces)
                    {
                        options.Directives.Remove(replace);
                    }

                    options.Directives.Add(directive);
                }
            }
        }
    }
}
