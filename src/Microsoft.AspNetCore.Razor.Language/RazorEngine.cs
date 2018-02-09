// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language.Extensions;

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class RazorEngine
    {
        public static RazorEngine Create()
        {
            return Create(configure: null);
        }

        public static RazorEngine Create(Action<IRazorEngineBuilder> configure) => CreateCore(RazorConfiguration.Default, false, configure);

        public static RazorEngine CreateDesignTime()
        {
            return CreateDesignTime(configure: null);
        }

        public static RazorEngine CreateDesignTime(Action<IRazorEngineBuilder> configure) => CreateCore(RazorConfiguration.Default, true, configure);

        // Internal since RazorEngine APIs are going to be obsolete.
        internal static RazorEngine CreateCore(RazorConfiguration configuration, bool designTime, Action<IRazorEngineBuilder> configure)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var builder = new DefaultRazorEngineBuilder(designTime);
            AddDefaults(builder);

            if (designTime)
            {
                AddDefaultDesignTimeFeatures(configuration, builder.Features);
            }
            else
            {
                AddDefaultRuntimeFeatures(configuration, builder.Features);
            }

            configure?.Invoke(builder);
            return builder.Build();
        }

        public static RazorEngine CreateEmpty(Action<IRazorEngineBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var builder = new DefaultRazorEngineBuilder(designTime: false);
            configure(builder);
            return builder.Build();
        }

        public static RazorEngine CreateDesignTimeEmpty(Action<IRazorEngineBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var builder = new DefaultRazorEngineBuilder(designTime: true);
            configure(builder);
            return builder.Build();
        }

        internal static void AddDefaults(IRazorEngineBuilder builder)
        {
            AddDefaultPhases(builder.Phases);
            AddDefaultFeatures(builder.Features);
        }

        internal static void AddDefaultPhases(IList<IRazorEnginePhase> phases)
        {
            phases.Add(new DefaultRazorParsingPhase());
            phases.Add(new DefaultRazorSyntaxTreePhase());
            phases.Add(new DefaultRazorTagHelperBinderPhase());
            phases.Add(new DefaultRazorIntermediateNodeLoweringPhase());
            phases.Add(new DefaultRazorDocumentClassifierPhase());
            phases.Add(new DefaultRazorDirectiveClassifierPhase());
            phases.Add(new DefaultRazorOptimizationPhase());
            phases.Add(new DefaultRazorCSharpLoweringPhase());
        }

        internal static void AddDefaultFeatures(ICollection<IRazorEngineFeature> features)
        {
            // General extensibility
            features.Add(new DefaultRazorDirectiveFeature());
            var targetExtensionFeature = new DefaultRazorTargetExtensionFeature();
            features.Add(targetExtensionFeature);
            features.Add(new DefaultMetadataIdentifierFeature());

            // Syntax Tree passes
            features.Add(new DefaultDirectiveSyntaxTreePass());
            features.Add(new HtmlNodeOptimizationPass());

            // Intermediate Node Passes
            features.Add(new DefaultDocumentClassifierPass());
            features.Add(new MetadataAttributePass());
            features.Add(new DirectiveRemovalOptimizationPass());
            features.Add(new DefaultTagHelperOptimizationPass());

            // Default Code Target Extensions
            targetExtensionFeature.TargetExtensions.Add(new MetadataAttributeTargetExtension());

            // Default configuration
            var configurationFeature = new DefaultDocumentClassifierPassFeature();
            configurationFeature.ConfigureClass.Add((document, @class) =>
            {
                @class.ClassName = "Template";
                @class.Modifiers.Add("public");
            });

            configurationFeature.ConfigureNamespace.Add((document, @namespace) =>
            {
                @namespace.Content = "Razor";
            });

            configurationFeature.ConfigureMethod.Add((document, method) =>
            {
                method.MethodName = "ExecuteAsync";
                method.ReturnType = $"global::{typeof(Task).FullName}";

                method.Modifiers.Add("public");
                method.Modifiers.Add("async");
                method.Modifiers.Add("override");
            });

            features.Add(configurationFeature);
        }

        internal static void AddDefaultRuntimeFeatures(RazorConfiguration configuration, ICollection<IRazorEngineFeature> features)
        {
            // Configure options
            features.Add(new DefaultRazorParserOptionsFeature(designTime: false, version: configuration.LanguageVersion));
            features.Add(new DefaultRazorCodeGenerationOptionsFeature(designTime: false));

            // Intermediate Node Passes
            features.Add(new PreallocatedTagHelperAttributeOptimizationPass());

            // Code Target Extensions
            var targetExtension = features.OfType<IRazorTargetExtensionFeature>().FirstOrDefault();
            Debug.Assert(targetExtension != null);

            targetExtension.TargetExtensions.Add(new DefaultTagHelperTargetExtension());
            targetExtension.TargetExtensions.Add(new PreallocatedAttributeTargetExtension());
        }

        internal static void AddDefaultDesignTimeFeatures(RazorConfiguration configuration, ICollection<IRazorEngineFeature> features)
        {
            // Configure options
            features.Add(new DefaultRazorParserOptionsFeature(designTime: true, version: configuration.LanguageVersion));
            features.Add(new DefaultRazorCodeGenerationOptionsFeature(designTime: true));
            features.Add(new SuppressChecksumOptionsFeature());

            // Intermediate Node Passes
            features.Add(new DesignTimeDirectivePass());

            // Code Target Extensions
            var targetExtension = features.OfType<IRazorTargetExtensionFeature>().FirstOrDefault();
            Debug.Assert(targetExtension != null);

            targetExtension.TargetExtensions.Add(new DefaultTagHelperTargetExtension());
            targetExtension.TargetExtensions.Add(new DesignTimeDirectiveTargetExtension());
        }

        public abstract IReadOnlyList<IRazorEngineFeature> Features { get; }

        public abstract IReadOnlyList<IRazorEnginePhase> Phases { get; }

        public abstract void Process(RazorCodeDocument document);
    }
}

