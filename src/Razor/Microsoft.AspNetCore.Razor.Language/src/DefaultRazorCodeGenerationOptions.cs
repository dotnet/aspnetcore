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
            string rootNamespace,
            bool suppressChecksum,
            bool suppressMetadataAttributes,
            bool suppressPrimaryMethodBody,
            bool suppressNullabilityEnforcement,
            bool omitMinimizedComponentAttributeValues,
            bool useEnhancedLinePragma)
        {
            IndentWithTabs = indentWithTabs;
            IndentSize = indentSize;
            DesignTime = designTime;
            RootNamespace = rootNamespace;
            SuppressChecksum = suppressChecksum;
            SuppressMetadataAttributes = suppressMetadataAttributes;
            SuppressPrimaryMethodBody = suppressPrimaryMethodBody;
            SuppressNullabilityEnforcement = suppressNullabilityEnforcement;
            OmitMinimizedComponentAttributeValues = omitMinimizedComponentAttributeValues;
            UseEnhancedLinePragma = useEnhancedLinePragma;
        }

        public override bool DesignTime { get; }

        public override bool IndentWithTabs { get; }

        public override int IndentSize { get; }

        public override string RootNamespace { get; }

        public override bool SuppressChecksum { get; }

        public override bool SuppressNullabilityEnforcement { get; }

        public override bool OmitMinimizedComponentAttributeValues { get; }

        public override bool UseEnhancedLinePragma { get; }
    }
}
