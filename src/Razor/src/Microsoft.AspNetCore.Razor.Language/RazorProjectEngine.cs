// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language.Components;
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

        internal virtual RazorCodeDocument Process(RazorSourceDocument source, IReadOnlyList<RazorSourceDocument> importSources, IReadOnlyList<TagHelperDescriptor> tagHelpers)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var codeDocument = CreateCodeDocumentCore(source, importSources, tagHelpers);
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

        internal virtual RazorCodeDocument ProcessDesignTime(RazorSourceDocument source, IReadOnlyList<RazorSourceDocument> importSources, IReadOnlyList<TagHelperDescriptor> tagHelpers)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var codeDocument = CreateCodeDocumentDesignTimeCore(source, importSources, tagHelpers);
            ProcessCore(codeDocument);
            return codeDocument;
        }

        protected abstract RazorCodeDocument CreateCodeDocumentCore(RazorProjectItem projectItem);

        internal virtual RazorCodeDocument CreateCodeDocumentCore(RazorSourceDocument source, IReadOnlyList<RazorSourceDocument> importSources, IReadOnlyList<TagHelperDescriptor> tagHelpers)
        {
            return RazorCodeDocument.Create(source, importSources);
        }

        protected abstract RazorCodeDocument CreateCodeDocumentDesignTimeCore(RazorProjectItem projectItem);

        internal virtual RazorCodeDocument CreateCodeDocumentDesignTimeCore(RazorSourceDocument source, IReadOnlyList<RazorSourceDocument> importSources, IReadOnlyList<TagHelperDescriptor> tagHelpers)
        {
            return RazorCodeDocument.Create(source, importSources);
        }

        protected abstract void ProcessCore(RazorCodeDocument codeDocument);

        internal static RazorProjectEngine CreateEmpty(Action<RazorProjectEngineBuilder> configure = null)
        {
            var builder = new DefaultRazorProjectEngineBuilder(RazorConfiguration.Default, RazorProjectFileSystem.Empty);

            configure?.Invoke(builder);

            return builder.Build();
        }

        internal static RazorProjectEngine Create() => Create(configure: null);

        internal static RazorProjectEngine Create(Action<RazorProjectEngineBuilder> configure) => Create(RazorConfiguration.Default, RazorProjectFileSystem.Empty, configure);

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

            // The intialization order is somewhat important.
            //
            // Defaults -> Extensions -> Additional customization
            //
            // This allows extensions to rely on default features, and customizations to override choices made by
            // extensions.
            AddDefaultPhases(builder.Phases);
            AddDefaultsFeatures(builder.Features);

            LoadExtensions(builder, configuration.Extensions);

            configure?.Invoke(builder);

            return builder.Build();
        }

        private static void AddDefaultPhases(IList<IRazorEnginePhase> phases)
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

            // Intermediate Node Passes
            features.Add(new DefaultDocumentClassifierPass());
            features.Add(new MetadataAttributePass());
            features.Add(new DesignTimeDirectivePass());
            features.Add(new DirectiveRemovalOptimizationPass());
            features.Add(new DefaultTagHelperOptimizationPass());
            features.Add(new PreallocatedTagHelperAttributeOptimizationPass());
            features.Add(new EliminateMethodBodyPass());

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

        private static void LoadExtensions(RazorProjectEngineBuilder builder, IReadOnlyList<RazorExtension> extensions)
        {
            for (var i = 0; i < extensions.Count; i++)
            {
                // For now we only handle AssemblyExtension - which is not user-constructable. We're keeping a tight
                // lid on how things work until we add official support for extensibility everywhere. So, this is
                // intentionally inflexible for the time being.
                if (extensions[i] is AssemblyExtension extension)
                {
                    var initializer = extension.CreateInitializer();
                    initializer?.Initialize(builder);
                }
            }

            // Default extensions.
            ComponentExtensions.Register(builder);
        }
    }
}
