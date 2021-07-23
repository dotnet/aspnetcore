// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
