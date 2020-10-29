// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor
{
    internal class ConfigureRazorCodeGenerationOptions : RazorEngineFeatureBase, IConfigureRazorCodeGenerationOptionsFeature
    {
        private readonly Action<RazorCodeGenerationOptionsBuilder> _action;

        public ConfigureRazorCodeGenerationOptions(Action<RazorCodeGenerationOptionsBuilder> action)
        {
            _action = action;
        }

        public int Order { get; set; }

        public void Configure(RazorCodeGenerationOptionsBuilder options) => _action(options);
    }
}
