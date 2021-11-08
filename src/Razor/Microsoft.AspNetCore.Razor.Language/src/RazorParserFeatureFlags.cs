// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language;

internal abstract class RazorParserFeatureFlags
{
    public static RazorParserFeatureFlags Create(RazorLanguageVersion version, string fileKind)
    {
        if (fileKind == null)
        {
            throw new ArgumentNullException(nameof(fileKind));
        }

        var allowMinimizedBooleanTagHelperAttributes = false;
        var allowHtmlCommentsInTagHelpers = false;
        var allowComponentFileKind = false;
        var allowRazorInAllCodeBlocks = false;
        var allowUsingVariableDeclarations = false;
        var allowConditionalDataDashAttributes = false;
        var allowCSharpInMarkupAttributeArea = true;
        var allowNullableForgivenessOperator = false;

        if (version.CompareTo(RazorLanguageVersion.Version_2_1) >= 0)
        {
            // Added in 2.1
            allowMinimizedBooleanTagHelperAttributes = true;
            allowHtmlCommentsInTagHelpers = true;
        }

        if (version.CompareTo(RazorLanguageVersion.Version_3_0) >= 0)
        {
            // Added in 3.0
            allowComponentFileKind = true;
            allowRazorInAllCodeBlocks = true;
            allowUsingVariableDeclarations = true;
            allowNullableForgivenessOperator = true;
        }

        if (FileKinds.IsComponent(fileKind))
        {
            allowConditionalDataDashAttributes = true;
            allowCSharpInMarkupAttributeArea = false;
        }

        if (version.CompareTo(RazorLanguageVersion.Experimental) >= 0)
        {
            allowConditionalDataDashAttributes = true;
        }

        return new DefaultRazorParserFeatureFlags(
            allowMinimizedBooleanTagHelperAttributes,
            allowHtmlCommentsInTagHelpers,
            allowComponentFileKind,
            allowRazorInAllCodeBlocks,
            allowUsingVariableDeclarations,
            allowConditionalDataDashAttributes,
            allowCSharpInMarkupAttributeArea,
            allowNullableForgivenessOperator);
    }

    public abstract bool AllowMinimizedBooleanTagHelperAttributes { get; }

    public abstract bool AllowHtmlCommentsInTagHelpers { get; }

    public abstract bool AllowComponentFileKind { get; }

    public abstract bool AllowRazorInAllCodeBlocks { get; }

    public abstract bool AllowUsingVariableDeclarations { get; }

    public abstract bool AllowConditionalDataDashAttributes { get; }

    public abstract bool AllowCSharpInMarkupAttributeArea { get; }

    public abstract bool AllowNullableForgivenessOperator { get; }

    private class DefaultRazorParserFeatureFlags : RazorParserFeatureFlags
    {
        public DefaultRazorParserFeatureFlags(
            bool allowMinimizedBooleanTagHelperAttributes,
            bool allowHtmlCommentsInTagHelpers,
            bool allowComponentFileKind,
            bool allowRazorInAllCodeBlocks,
            bool allowUsingVariableDeclarations,
            bool allowConditionalDataDashAttributesInComponents,
            bool allowCSharpInMarkupAttributeArea,
            bool allowNullableForgivenessOperator)
        {
            AllowMinimizedBooleanTagHelperAttributes = allowMinimizedBooleanTagHelperAttributes;
            AllowHtmlCommentsInTagHelpers = allowHtmlCommentsInTagHelpers;
            AllowComponentFileKind = allowComponentFileKind;
            AllowRazorInAllCodeBlocks = allowRazorInAllCodeBlocks;
            AllowUsingVariableDeclarations = allowUsingVariableDeclarations;
            AllowConditionalDataDashAttributes = allowConditionalDataDashAttributesInComponents;
            AllowCSharpInMarkupAttributeArea = allowCSharpInMarkupAttributeArea;
            AllowNullableForgivenessOperator = allowNullableForgivenessOperator;
        }

        public override bool AllowMinimizedBooleanTagHelperAttributes { get; }

        public override bool AllowHtmlCommentsInTagHelpers { get; }

        public override bool AllowComponentFileKind { get; }

        public override bool AllowRazorInAllCodeBlocks { get; }

        public override bool AllowUsingVariableDeclarations { get; }

        public override bool AllowConditionalDataDashAttributes { get; }

        public override bool AllowCSharpInMarkupAttributeArea { get; }

        public override bool AllowNullableForgivenessOperator { get; }
    }
}
