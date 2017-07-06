// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DesignTimeOptionsFeature : RazorEngineFeatureBase, IRazorCodeGenerationOptionsFeature
    {
        public int Order { get; set; }

        public void Configure(RazorCodeGenerationOptionsBuilder options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.DesignTime = true;
            options.SuppressChecksum = true;
        }
    }
}
