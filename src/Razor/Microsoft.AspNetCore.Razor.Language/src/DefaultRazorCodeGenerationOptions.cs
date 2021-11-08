// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

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
        bool supportLocalizedComponentNames,
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
        SupportLocalizedComponentNames = supportLocalizedComponentNames;
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
