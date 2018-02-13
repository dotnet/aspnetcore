// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language.Extensions;

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class RazorProjectEngine
    {
        public abstract RazorConfiguration Configuration { get; }

        public abstract RazorProjectFileSystem FileSystem { get; }

        public abstract RazorEngine Engine { get; }

        public IReadOnlyList<IRazorEngineFeature> EngineFeatures => Engine.Features;

        public IReadOnlyList<IRazorEnginePhase> Phases => Engine.Phases;

        public abstract IReadOnlyList<IRazorProjectEngineFeature> ProjectFeatures { get; }

        public virtual RazorCodeDocument Process(RazorProjectItem projectItem)
        {
            if (projectItem == null)
            {
                throw new ArgumentNullException(nameof(projectItem));
            }

            var codeDocument = CreateCodeDocumentCore(projectItem);
            ProcessCore(codeDocument);
            return codeDocument;
        }

        public virtual RazorCodeDocument ProcessDesignTime(RazorProjectItem projectItem)
        {
            if (projectItem == null)
            {
                throw new ArgumentNullException(nameof(projectItem));
            }

            var codeDocument = CreateCodeDocumentDesignTimeCore(projectItem);
            ProcessCore(codeDocument);
            return codeDocument;
        }

        protected abstract RazorCodeDocument CreateCodeDocumentCore(RazorProjectItem projectItem);

        protected abstract RazorCodeDocument CreateCodeDocumentDesignTimeCore(RazorProjectItem projectItem);

        protected abstract void ProcessCore(RazorCodeDocument codeDocument);

        public static RazorProjectEngine Create(RazorConfiguration configuration, RazorProjectFileSystem fileSystem) => Create(configuration, fileSystem, configure: null);

        public static RazorProjectEngine Create(
            RazorConfiguration configuration,
            RazorProjectFileSystem fileSystem,
            Action<RazorProjectEngineBuilder> configure)
        {
            if (fileSystem == null)
            {
                throw new ArgumentNullException(nameof(fileSystem));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var builder = new DefaultRazorProjectEngineBuilder(configuration, fileSystem);

            RazorEngine.AddDefaultPhases(builder.Phases);
            AddDefaultsFeatures(builder.Features);

            configure?.Invoke(builder);

            return builder.Build();
        }
        
        private static void AddDefaultsFeatures(ICollection<IRazorFeature> features)
        {
            features.Add(new DefaultImportProjectFeature());

            // General extensibility
            features.Add(new DefaultRazorDirectiveFeature());
            features.Add(new DefaultMetadataIdentifierFeature());

            // Options features
            features.Add(new DefaultRazorParserOptionsFactoryProjectFeature());
            features.Add(new DefaultRazorCodeGenerationOptionsFactoryProjectFeature());

            // Legacy options features
            //
            // These features are obsolete as of 2.1. Our code will resolve this but not invoke them.
            features.Add(new DefaultRazorParserOptionsFeature(designTime: false, version: RazorLanguageVersion.Version_2_0));
            features.Add(new DefaultRazorCodeGenerationOptionsFeature(designTime: false));

            // Syntax Tree passes
            features.Add(new DefaultDirectiveSyntaxTreePass());
            features.Add(new HtmlNodeOptimizationPass());
            features.Add(new PreallocatedTagHelperAttributeOptimizationPass());

            // Intermediate Node Passes
            features.Add(new DefaultDocumentClassifierPass());
            features.Add(new MetadataAttributePass());
            features.Add(new DesignTimeDirectivePass());
            features.Add(new DirectiveRemovalOptimizationPass());
            features.Add(new DefaultTagHelperOptimizationPass());

            // Default Code Target Extensions
            var targetExtensionFeature = new DefaultRazorTargetExtensionFeature();
            features.Add(targetExtensionFeature);
            targetExtensionFeature.TargetExtensions.Add(new MetadataAttributeTargetExtension());
            targetExtensionFeature.TargetExtensions.Add(new DefaultTagHelperTargetExtension());
            targetExtensionFeature.TargetExtensions.Add(new PreallocatedAttributeTargetExtension());
            targetExtensionFeature.TargetExtensions.Add(new DesignTimeDirectiveTargetExtension());

            // Default configuration
            var configurationFeature = new DefaultDocumentClassifierPassFeature();
            features.Add(configurationFeature);
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
        }

        internal static void AddDefaultRuntimeFeatures(RazorConfiguration configuration, ICollection<IRazorEngineFeature> features)
        {
            // Configure options
            features.Add(new DefaultRazorParserOptionsFeature(designTime: false, version: configuration.LanguageVersion));
            features.Add(new DefaultRazorCodeGenerationOptionsFeature(designTime: false));
        }

        internal static void AddDefaultDesignTimeFeatures(RazorConfiguration configuration, ICollection<IRazorEngineFeature> features)
        {
            // Configure options
            features.Add(new DefaultRazorParserOptionsFeature(designTime: true, version: configuration.LanguageVersion));
            features.Add(new DefaultRazorCodeGenerationOptionsFeature(designTime: true));
            features.Add(new SuppressChecksumOptionsFeature());
        }
    }
}
