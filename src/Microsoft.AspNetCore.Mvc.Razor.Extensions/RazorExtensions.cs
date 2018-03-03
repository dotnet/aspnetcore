// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
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
            NamespaceDirective.Register(builder);
            PageDirective.Register(builder);

            FunctionsDirective.Register(builder);
            InheritsDirective.Register(builder);
            SectionDirective.Register(builder);

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

            builder.SetImportFeature(new MvcImportProjectFeature());
        }

        #region Obsolete
        public static void Register(IRazorEngineBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            InjectDirective.Register(builder);
            ModelDirective.Register(builder);
            NamespaceDirective.Register(builder);
            PageDirective.Register(builder);

            FunctionsDirective.Register(builder);
            InheritsDirective.Register(builder);
            SectionDirective.Register(builder);

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

            if (!builder.DesignTime)
            {
                builder.Features.Add(new AssemblyAttributeInjectionPass());
                builder.Features.Add(new InstrumentationPass());
            }
        }
        #endregion
    }
}
