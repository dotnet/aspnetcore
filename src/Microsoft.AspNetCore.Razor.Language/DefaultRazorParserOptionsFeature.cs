// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRazorParserOptionsFeature : RazorEngineFeatureBase, IRazorParserOptionsFeature
    {
        private readonly bool _designTime;
        private IConfigureRazorParserOptionsFeature[] _configureOptions;

        public DefaultRazorParserOptionsFeature(bool designTime)
        {
            _designTime = designTime;
        }

        protected override void OnInitialized()
        {
            _configureOptions = Engine.Features.OfType<IConfigureRazorParserOptionsFeature>().ToArray();
        }

        public RazorParserOptions GetOptions()
        {
            var builder = new DefaultRazorParserOptionsBuilder(_designTime);
            for (var i = 0; i < _configureOptions.Length; i++)
            {
                _configureOptions[i].Configure(builder);
            }

            var options = builder.Build();

            return options;
        }
    }
}
