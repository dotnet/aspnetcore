// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRazorDirectiveFeature : RazorEngineFeatureBase, IRazorDirectiveFeature, IConfigureRazorParserOptionsFeature
    {
        public ICollection<DirectiveDescriptor> Directives { get; } = new List<DirectiveDescriptor>();

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
        }
    }
}
