// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation
{
    internal class RazorViewCompilerProvider : IViewCompilerProvider
    {
        private readonly RazorViewCompiler _compiler;

        public RazorViewCompilerProvider(
            ApplicationPartManager applicationPartManager,
            ILoggerFactory loggerFactory)
        {
            var feature = new ViewsFeature();
            applicationPartManager.PopulateFeature(feature);

            _compiler = new RazorViewCompiler(feature.ViewDescriptors, loggerFactory.CreateLogger<RazorViewCompiler>());
        }

        public IViewCompiler GetCompiler() => _compiler;
    }
}
