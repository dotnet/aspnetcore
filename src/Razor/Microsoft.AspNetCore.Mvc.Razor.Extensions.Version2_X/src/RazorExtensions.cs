// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X;

public static class RazorExtensions
{
    public static void Register(RazorProjectEngineBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        FunctionsDirective.Register(builder);
        InjectDirective.Register(builder);
        ModelDirective.Register(builder);
        NamespaceDirective.Register(builder);
        PageDirective.Register(builder);

        InheritsDirective.Register(builder);
        SectionDirective.Register(builder);

        builder.Features.Add(new DefaultTagHelperDescriptorProvider());
        builder.Features.Add(new ViewComponentTagHelperDescriptorProvider());

        builder.AddTargetExtension(new ViewComponentTagHelperTargetExtension());
        builder.AddTargetExtension(new TemplateTargetExtension()
        {
            TemplateTypeName = "global::Microsoft.AspNetCore.Mvc.Razor.HelperResult",
        });

        builder.Features.Add(new ModelExpressionPass());
        builder.Features.Add(new PagesPropertyInjectionPass());
        builder.Features.Add(new ViewComponentTagHelperPass());
        builder.Features.Add(new RazorPageDocumentClassifierPass());
        builder.Features.Add(new MvcViewDocumentClassifierPass());
        builder.Features.Add(new AssemblyAttributeInjectionPass());
        builder.Features.Add(new InstrumentationPass());

        builder.Features.Add(new MvcImportProjectFeature());

        // The default C# language version for what this Razor configuration supports.
        builder.SetCSharpLanguageVersion(LanguageVersion.CSharp7_3);
    }
}
