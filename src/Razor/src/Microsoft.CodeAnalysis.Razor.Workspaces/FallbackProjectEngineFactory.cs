// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor
{
    [Export(typeof(IFallbackProjectEngineFactory))]
    internal class FallbackProjectEngineFactory : IFallbackProjectEngineFactory
    {
        public RazorProjectEngine Create(RazorConfiguration configuration, RazorProjectFileSystem fileSystem, Action<RazorProjectEngineBuilder> configure)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (fileSystem == null)
            {
                throw new ArgumentNullException(nameof(fileSystem));
            }

            // This is a very basic implementation that will provide reasonable support without crashing.
            // If the user falls into this situation, ideally they can realize that something is wrong and take
            // action.
            //
            // This has no support for:
            // - Tag Helpers
            // - Imports
            // - Default Imports
            // - and will have a very limited set of directives
            return RazorProjectEngine.Create(configuration, fileSystem, configure);
        }
    }
}
