// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version1_X;

public static class RazorExtensions
{
    public static void Register(RazorProjectEngineBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        InjectDirective.Register(builder);
        ModelDirective.Register(builder);

        InheritsDirective.Register(builder);

        builder.Features.Add(new DefaultTagHelperDescriptorProvider());

        // Register section directive with the 1.x compatible target extension.
        builder.AddDirective(SectionDirective.Directive);
        builder.Features.Add(new SectionDirectivePass());
        builder.AddTargetExtension(new LegacySectionTargetExtension());

        builder.AddTargetExtension(new TemplateTargetExtension()
        {
            TemplateTypeName = "global::Microsoft.AspNetCore.Mvc.Razor.HelperResult",
        });

        builder.Features.Add(new ModelExpressionPass());
        builder.Features.Add(new MvcViewDocumentClassifierPass());

        builder.Features.Add(new MvcImportProjectFeature());

        // The default C# language version for what this Razor configuration supports.
        builder.SetCSharpLanguageVersion(LanguageVersion.CSharp7_3);
    }

    public static void RegisterViewComponentTagHelpers(RazorProjectEngineBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Features.Add(new ViewComponentTagHelperDescriptorProvider());

        builder.Features.Add(new ViewComponentTagHelperPass());
        builder.AddTargetExtension(new ViewComponentTagHelperTargetExtension());
    }

    #region Obsolete
    [Obsolete("This method is obsolete and will be removed in a future version.")]
    public static void Register(IRazorEngineBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        EnsureDesignTime(builder);

        InjectDirective.Register(builder);
        ModelDirective.Register(builder);

        FunctionsDirective.Register(builder);
        InheritsDirective.Register(builder);

        builder.Features.Add(new DefaultTagHelperDescriptorProvider());

        // Register section directive with the 1.x compatible target extension.
        builder.AddDirective(SectionDirective.Directive);
        builder.Features.Add(new SectionDirectivePass());
        builder.AddTargetExtension(new LegacySectionTargetExtension());

        builder.AddTargetExtension(new TemplateTargetExtension()
        {
            TemplateTypeName = "global::Microsoft.AspNetCore.Mvc.Razor.HelperResult",
        });

        builder.Features.Add(new ModelExpressionPass());
        builder.Features.Add(new MvcViewDocumentClassifierPass());
    }

    [Obsolete("This method is obsolete and will be removed in a future version.")]
    public static void RegisterViewComponentTagHelpers(IRazorEngineBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        EnsureDesignTime(builder);

        builder.Features.Add(new ViewComponentTagHelperDescriptorProvider());
        builder.Features.Add(new ViewComponentTagHelperPass());
        builder.AddTargetExtension(new ViewComponentTagHelperTargetExtension());
    }

#pragma warning disable CS0618 // Type or member is obsolete
    private static void EnsureDesignTime(IRazorEngineBuilder builder)
#pragma warning restore CS0618 // Type or member is obsolete
    {
        if (builder.DesignTime)
        {
            return;
        }

        throw new NotSupportedException(Resources.RuntimeCodeGenerationNotSupported);
    }
    #endregion
}
