// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Evolution.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public abstract class RazorEngine
    {
        public static RazorEngine Create()
        {
            return Create(configure: null);
        }

        public static RazorEngine Create(Action<IRazorEngineBuilder> configure)
        {
            var builder = new DefaultRazorEngineBuilder();
            AddDefaults(builder);
            AddRuntimeDefaults(builder);
            configure?.Invoke(builder);
            return builder.Build();
        }

        public static RazorEngine CreateDesignTime()
        {
            return CreateDesignTime(configure: null);
        }

        public static RazorEngine CreateDesignTime(Action<IRazorEngineBuilder> configure)
        {
            var builder = new DefaultRazorEngineBuilder()
            {
                DesignTime = true,
            };
            AddDefaults(builder);
            AddDesignTimeDefaults(builder);
            configure?.Invoke(builder);
            return builder.Build();
        }

        public static RazorEngine CreateEmpty(Action<IRazorEngineBuilder> configure)
        {
            var builder = new DefaultRazorEngineBuilder();
            configure?.Invoke(builder);
            return builder.Build();
        }

        internal static void AddDefaults(IRazorEngineBuilder builder)
        {
            builder.Phases.Add(new DefaultRazorParsingPhase());
            builder.Phases.Add(new DefaultRazorSyntaxTreePhase());
            builder.Phases.Add(new DefaultRazorIRLoweringPhase());
            builder.Phases.Add(new DefaultRazorDocumentClassifierPhase());
            builder.Phases.Add(new DefaultRazorDirectiveClassifierPhase());
            builder.Phases.Add(new DefaultRazorIROptimizationPhase());
            builder.Phases.Add(new DefaultRazorCSharpLoweringPhase());

            // General extensibility
            builder.Features.Add(new DefaultRazorDirectiveFeature());
            builder.Features.Add(new DefaultRazorTargetExtensionFeature());

            // Syntax Tree passes
            builder.Features.Add(new DefaultDirectiveSyntaxTreePass());
            builder.Features.Add(new HtmlNodeOptimizationPass());
            builder.Features.Add(new TagHelperBinderSyntaxTreePass());

            // IR Passes
            builder.Features.Add(new DefaultDocumentClassifierPass());
            builder.Features.Add(new DefaultDirectiveIRPass());
            builder.Features.Add(new DirectiveRemovalIROptimizationPass());

            // Default Runtime Targets
            builder.AddTargetExtension(new TemplateTargetExtension());
        }

        internal static void AddRuntimeDefaults(IRazorEngineBuilder builder)
        {
            builder.Features.Add(new RazorPreallocatedTagHelperAttributeOptimizationPass());
        }

        internal static void AddDesignTimeDefaults(IRazorEngineBuilder builder)
        {
            builder.Features.Add(new ConfigureDesignTimeOptions());
            builder.Features.Add(new RazorDesignTimeIRPass());
        }

        public abstract IReadOnlyList<IRazorEngineFeature> Features { get; }

        public abstract IReadOnlyList<IRazorEnginePhase> Phases { get; }

        public abstract void Process(RazorCodeDocument document);

        internal class ConfigureDesignTimeOptions : IRazorConfigureParserFeature
        {
            public RazorEngine Engine { get; set; }

            public int Order { get; set; }

            public void Configure(RazorParserOptions options)
            {
                options.DesignTimeMode = true;
            }
        }
    }
}
