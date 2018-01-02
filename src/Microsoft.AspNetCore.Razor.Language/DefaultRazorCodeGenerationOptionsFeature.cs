// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRazorCodeGenerationOptionsFeature : RazorEngineFeatureBase, IRazorCodeGenerationOptionsFeature
    {
        private readonly bool _designTime;
        private IConfigureRazorCodeGenerationOptionsFeature[] _configureOptions;

        public DefaultRazorCodeGenerationOptionsFeature(bool designTime)
        {
            _designTime = designTime;
        }

        protected override void OnInitialized()
        {
            _configureOptions = Engine.Features.OfType<IConfigureRazorCodeGenerationOptionsFeature>().ToArray();
        }

        public RazorCodeGenerationOptions GetOptions()
        {
            return _designTime ? RazorCodeGenerationOptions.CreateDesignTime(ConfigureOptions) : RazorCodeGenerationOptions.Create(ConfigureOptions);
        }

        private void ConfigureOptions(RazorCodeGenerationOptionsBuilder builder)
        {
            for (var i = 0; i < _configureOptions.Length; i++)
            {
                _configureOptions[i].Configure(builder);
            }
        }
    }
}
