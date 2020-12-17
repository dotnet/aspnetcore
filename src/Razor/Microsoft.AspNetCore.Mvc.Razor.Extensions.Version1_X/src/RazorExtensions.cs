// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version1_X
{
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
    }
}
