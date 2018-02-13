// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRazorProjectEngine : RazorProjectEngine
    {
        public DefaultRazorProjectEngine(
            RazorConfiguration configuration,
            RazorEngine engine,
            RazorProjectFileSystem fileSystem,
            IReadOnlyList<IRazorProjectEngineFeature> projectFeatures)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (engine == null)
            {
                throw new ArgumentNullException(nameof(engine));
            }

            if (fileSystem == null)
            {
                throw new ArgumentNullException(nameof(fileSystem));
            }

            if (projectFeatures == null)
            {
                throw new ArgumentNullException(nameof(projectFeatures));
            }

            Configuration = configuration;
            Engine = engine;
            FileSystem = fileSystem;
            ProjectFeatures = projectFeatures;

            for (var i = 0; i < projectFeatures.Count; i++)
            {
                projectFeatures[i].ProjectEngine = this;
            }
        }

        public override RazorConfiguration Configuration { get; }

        public override RazorProjectFileSystem FileSystem { get; }

        public override RazorEngine Engine { get; }

        public override IReadOnlyList<IRazorProjectEngineFeature> ProjectFeatures { get; }

        protected override void ConfigureParserOptions(RazorParserOptionsBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
        }

        protected override void ConfigureDesignTimeParserOptions(RazorParserOptionsBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.SetDesignTime(true);
        }

        protected override void ConfigureCodeGenerationOptions(RazorCodeGenerationOptionsBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
        }

        protected override void ConfigureDesignTimeCodeGenerationOptions(RazorCodeGenerationOptionsBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.SetDesignTime(true);
            builder.SuppressChecksum = true;
            builder.SuppressMetadataAttributes = true;
        }

        protected override RazorCodeDocument ProcessCore(
            RazorProjectItem projectItem,
            Action<RazorParserOptionsBuilder> configureParser,
            Action<RazorCodeGenerationOptionsBuilder> configureCodeGeneration)
        {
            if (projectItem == null)
            {
                throw new ArgumentNullException(nameof(projectItem));
            }

            var sourceDocument = RazorSourceDocument.ReadFrom(projectItem);

            var importFeature = GetRequiredFeature<IImportProjectFeature>();
            var imports = importFeature.GetImports(projectItem);

            var parserOptions = GetRequiredFeature<IRazorParserOptionsFactoryProjectFeature>().Create(configureParser);
            var codeGenerationOptions = GetRequiredFeature<IRazorCodeGenerationOptionsFactoryProjectFeature>().Create(configureCodeGeneration);

            var codeDocument = RazorCodeDocument.Create(sourceDocument, imports, parserOptions, codeGenerationOptions);

            Engine.Process(codeDocument);

            return codeDocument;
        }

        private TFeature GetRequiredFeature<TFeature>() where TFeature : IRazorProjectEngineFeature
        {
            var feature = ProjectFeatures.OfType<TFeature>().FirstOrDefault();
            if (feature == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatRazorProjectEngineMissingFeatureDependency(
                        typeof(RazorProjectEngine).FullName,
                        typeof(TFeature).FullName));
            }

            return feature;
        }
    }
}
