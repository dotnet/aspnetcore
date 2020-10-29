// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRazorCodeGenerationOptionsBuilder : RazorCodeGenerationOptionsBuilder
    {
        private bool _designTime;

        public DefaultRazorCodeGenerationOptionsBuilder(RazorConfiguration configuration, string fileKind)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            Configuration = configuration;
            FileKind = fileKind;
        }

        public DefaultRazorCodeGenerationOptionsBuilder(bool designTime)
        {
            _designTime = designTime;
        }

        public override RazorConfiguration Configuration { get; }

        public override bool DesignTime => _designTime;

        public override string FileKind { get; }

        public override int IndentSize { get; set; } = 4;

        public override bool IndentWithTabs { get; set; }

        public override bool SuppressChecksum { get; set; }

        public override bool SuppressNullabilityEnforcement { get; set; }

        public override bool OmitMinimizedComponentAttributeValues { get; set; }

        public override RazorCodeGenerationOptions Build()
        {
            return new DefaultRazorCodeGenerationOptions(
                IndentWithTabs,
                IndentSize,
                DesignTime,
                RootNamespace,
                SuppressChecksum,
                SuppressMetadataAttributes,
                SuppressPrimaryMethodBody,
                SuppressNullabilityEnforcement,
                OmitMinimizedComponentAttributeValues)
            {
                GenerateDesignerIfDefs = GenerateDesignerIfDefs,
            };
        }

        public override void SetDesignTime(bool designTime)
        {
            _designTime = designTime;
        }
    }
}
