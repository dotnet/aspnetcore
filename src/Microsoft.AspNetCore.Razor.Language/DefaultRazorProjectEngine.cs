// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
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

        protected override RazorCodeDocument CreateCodeDocumentCore(RazorProjectItem projectItem)
        {
            if (projectItem == null)
            {
                throw new ArgumentNullException(nameof(projectItem));
            }

            var sourceDocument = RazorSourceDocument.ReadFrom(projectItem);

            var importFeature = GetRequiredFeature<IImportProjectFeature>();
            var importItems = importFeature.GetImports(projectItem);
            var importSourceDocuments = GetImportSourceDocuments(importItems);

            return CreateCodeDocumentCore(sourceDocument, importSourceDocuments, tagHelpers: null);
        }

        internal override RazorCodeDocument CreateCodeDocumentCore(RazorSourceDocument sourceDocument, IReadOnlyList<RazorSourceDocument> importSourceDocuments, IReadOnlyList<TagHelperDescriptor> tagHelpers)
        {
            if (sourceDocument == null)
            {
                throw new ArgumentNullException(nameof(sourceDocument));
            }

            var parserOptions = GetRequiredFeature<IRazorParserOptionsFactoryProjectFeature>().Create(ConfigureParserOptions);
            var codeGenerationOptions = GetRequiredFeature<IRazorCodeGenerationOptionsFactoryProjectFeature>().Create(ConfigureCodeGenerationOptions);

            var codeDocument = RazorCodeDocument.Create(sourceDocument, importSourceDocuments, parserOptions, codeGenerationOptions);
            codeDocument.SetTagHelpers(tagHelpers);

            return codeDocument;
        }

        protected override RazorCodeDocument CreateCodeDocumentDesignTimeCore(RazorProjectItem projectItem)
        {
            if (projectItem == null)
            {
                throw new ArgumentNullException(nameof(projectItem));
            }

            var sourceDocument = RazorSourceDocument.ReadFrom(projectItem);

            var importFeature = GetRequiredFeature<IImportProjectFeature>();
            var importItems = importFeature.GetImports(projectItem);
            var importSourceDocuments = GetImportSourceDocuments(importItems, suppressExceptions: true);

            return CreateCodeDocumentDesignTimeCore(sourceDocument, importSourceDocuments, tagHelpers: null);
        }

        internal override RazorCodeDocument CreateCodeDocumentDesignTimeCore(RazorSourceDocument sourceDocument, IReadOnlyList<RazorSourceDocument> importSourceDocuments, IReadOnlyList<TagHelperDescriptor> tagHelpers)
        {
            if (sourceDocument == null)
            {
                throw new ArgumentNullException(nameof(sourceDocument));
            }

            var parserOptions = GetRequiredFeature<IRazorParserOptionsFactoryProjectFeature>().Create(ConfigureDesignTimeParserOptions);
            var codeGenerationOptions = GetRequiredFeature<IRazorCodeGenerationOptionsFactoryProjectFeature>().Create(ConfigureDesignTimeCodeGenerationOptions);

            var codeDocument = RazorCodeDocument.Create(sourceDocument, importSourceDocuments, parserOptions, codeGenerationOptions);
            codeDocument.SetTagHelpers(tagHelpers);

            return codeDocument;
        }

        protected override void ProcessCore(RazorCodeDocument codeDocument)
        {
            if (codeDocument == null)
            {
                throw new ArgumentNullException(nameof(codeDocument));
            }

            Engine.Process(codeDocument);
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

        private void ConfigureParserOptions(RazorParserOptionsBuilder builder)
        {
        }

        private void ConfigureDesignTimeParserOptions(RazorParserOptionsBuilder builder)
        {
            builder.SetDesignTime(true);
        }

        private void ConfigureCodeGenerationOptions(RazorCodeGenerationOptionsBuilder builder)
        {
        }

        private void ConfigureDesignTimeCodeGenerationOptions(RazorCodeGenerationOptionsBuilder builder)
        {
            builder.SetDesignTime(true);
            builder.SuppressChecksum = true;
            builder.SuppressMetadataAttributes = true;
        }

        // Internal for testing
        internal static IReadOnlyList<RazorSourceDocument> GetImportSourceDocuments(
            IReadOnlyList<RazorProjectItem> importItems,
            bool suppressExceptions = false)
        {
            var imports = new List<RazorSourceDocument>();
            for (var i = 0; i < importItems.Count; i++)
            {
                var importItem = importItems[i];

                if (importItem.Exists)
                {
                    try
                    {
                        // Normal import, has file paths, content etc.
                        var sourceDocument = RazorSourceDocument.ReadFrom(importItem);
                        imports.Add(sourceDocument);
                    }
                    catch (IOException) when (suppressExceptions)
                    {
                        // Something happened when trying to read the item from disk.
                        // Catch the exception so we don't crash the editor.
                    }
                }
            }

            return imports;
        }
    }
}
