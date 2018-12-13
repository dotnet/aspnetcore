// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRazorCodeGenerationOptions : RazorCodeGenerationOptions
    {
        public DefaultRazorCodeGenerationOptions(
            bool indentWithTabs, 
            int indentSize, 
            bool designTime, 
            bool suppressChecksum,
            bool supressMetadataAttributes)
        {
            IndentWithTabs = indentWithTabs;
            IndentSize = indentSize;
            DesignTime = designTime;
            SuppressChecksum = suppressChecksum;
            SuppressMetadataAttributes = supressMetadataAttributes;
        }

        public override bool DesignTime { get; }

        public override bool IndentWithTabs { get; }

        public override int IndentSize { get; }

        public override bool SuppressChecksum { get; }
    }
}
