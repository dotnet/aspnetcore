// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRazorCodeGenerationOptionsBuilder : RazorCodeGenerationOptionsBuilder
    {
        public DefaultRazorCodeGenerationOptionsBuilder(bool designTime)
        {
            DesignTime = designTime;
        }

        public override bool DesignTime { get; }

        public override int IndentSize { get; set; } = 4;

        public override bool IndentWithTabs { get; set; }

        public override bool SuppressChecksum { get; set; }
        
        public override RazorCodeGenerationOptions Build()
        {
            return new DefaultRazorCodeGenerationOptions(IndentWithTabs, IndentSize, DesignTime, SuppressChecksum, SuppressMetadataAttributes);
        }
    }
}
