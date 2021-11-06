// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language;

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

    public override bool SupportLocalizedComponentNames { get; set; }

    public override bool UseEnhancedLinePragma { get; set; }

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
            OmitMinimizedComponentAttributeValues,
            SupportLocalizedComponentNames,
            UseEnhancedLinePragma)
        {
            SuppressMetadataSourceChecksumAttributes = SuppressMetadataSourceChecksumAttributes,
        };
    }

    public override void SetDesignTime(bool designTime)
    {
        _designTime = designTime;
    }
}
