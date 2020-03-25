// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation
{
    internal class DefaultViewCompilerProvider : IViewCompilerProvider
    {
        private readonly DefaultViewCompiler _compiler;

        public DefaultViewCompilerProvider(
            ApplicationPartManager applicationPartManager,
            ILoggerFactory loggerFactory)
        {
            var feature = new ViewsFeature();
            applicationPartManager.PopulateFeature(feature);

            _compiler = new DefaultViewCompiler(feature.ViewDescriptors, loggerFactory.CreateLogger<DefaultViewCompiler>());
        }

        public IViewCompiler GetCompiler() => _compiler;
    }
}
