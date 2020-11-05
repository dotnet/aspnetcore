// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRazorDirectiveFeature : RazorEngineFeatureBase, IRazorDirectiveFeature, IConfigureRazorParserOptionsFeature
    {
        // To maintain backwards compatibility, adding to this list will default to legacy file kind.
        public ICollection<DirectiveDescriptor> Directives
        {
            get
            {
                ICollection<DirectiveDescriptor> result;
                if (!DirectivesByFileKind.TryGetValue(FileKinds.Legacy, out result))
                {
                    result = new List<DirectiveDescriptor>();
                    DirectivesByFileKind.Add(FileKinds.Legacy, result);
                }

                return result;
            }
        }

        public IDictionary<string, ICollection<DirectiveDescriptor>> DirectivesByFileKind { get; } = new Dictionary<string, ICollection<DirectiveDescriptor>>(StringComparer.OrdinalIgnoreCase);

        public int Order => 100;

        void IConfigureRazorParserOptionsFeature.Configure(RazorParserOptionsBuilder options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.Directives.Clear();

            var fileKind = options.FileKind ?? FileKinds.Legacy;
            if (DirectivesByFileKind.TryGetValue(fileKind, out var directives))
            {
                foreach (var directive in directives)
                {
                    options.Directives.Add(directive);
                }
            }
        }
    }
}
