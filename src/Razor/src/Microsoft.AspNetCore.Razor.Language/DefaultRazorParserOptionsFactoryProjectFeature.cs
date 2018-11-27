// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRazorParserOptionsFactoryProjectFeature : RazorProjectEngineFeatureBase, IRazorParserOptionsFactoryProjectFeature
    {
        private IConfigureRazorParserOptionsFeature[] _configureOptions;

        protected override void OnInitialized()
        {
            _configureOptions = ProjectEngine.EngineFeatures.OfType<IConfigureRazorParserOptionsFeature>().ToArray();
        }

        public RazorParserOptions Create(Action<RazorParserOptionsBuilder> configure)
        {
            var builder = new DefaultRazorParserOptionsBuilder(ProjectEngine.Configuration);
            configure?.Invoke(builder);

            for (var i = 0; i < _configureOptions.Length; i++)
            {
                _configureOptions[i].Configure(builder);
            }
            
            var options = builder.Build();
            return options;
        }
    }
}