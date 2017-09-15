// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRazorParserOptionsFeature : RazorEngineFeatureBase, IRazorParserOptionsFeature
    {
        private readonly bool _designTime;
        private readonly RazorLanguageVersion _version;
        private IConfigureRazorParserOptionsFeature[] _configureOptions;

        public DefaultRazorParserOptionsFeature(bool designTime, RazorLanguageVersion version)
        {
            _designTime = designTime;
            _version = version;
        }

        protected override void OnInitialized()
        {
            _configureOptions = Engine.Features.OfType<IConfigureRazorParserOptionsFeature>().ToArray();
        }

        public RazorParserOptions GetOptions()
        {
            var builder = new DefaultRazorParserOptionsBuilder(_designTime, _version);
            for (var i = 0; i < _configureOptions.Length; i++)
            {
                _configureOptions[i].Configure(builder);
            }

            var options = builder.Build();

            return options;
        }
    }
}
