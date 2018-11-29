// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRazorCodeGenerationOptionsBuilder : RazorCodeGenerationOptionsBuilder
    {
        private bool _designTime;

        public DefaultRazorCodeGenerationOptionsBuilder(RazorConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
        }

        public DefaultRazorCodeGenerationOptionsBuilder(bool designTime)
        {
            _designTime = designTime;
        }

        public override RazorConfiguration Configuration { get; }

        public override bool DesignTime => _designTime;

        public override int IndentSize { get; set; } = 4;

        public override bool IndentWithTabs { get; set; }

        public override bool SuppressChecksum { get; set; }

        public override RazorCodeGenerationOptions Build()
        {
            return new DefaultRazorCodeGenerationOptions(
                IndentWithTabs,
                IndentSize,
                DesignTime,
                SuppressChecksum,
                SuppressMetadataAttributes,
                SuppressPrimaryMethodBody);
        }

        public override void SetDesignTime(bool designTime)
        {
            _designTime = designTime;
        }
    }
}
