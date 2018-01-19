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
            RazorEngine engine,
            RazorProjectFileSystem fileSystem,
            IReadOnlyList<IRazorProjectEngineFeature> features)
        {
            if (engine == null)
            {
                throw new ArgumentNullException(nameof(engine));
            }

            if (fileSystem == null)
            {
                throw new ArgumentNullException(nameof(fileSystem));
            }

            if (features == null)
            {
                throw new ArgumentNullException(nameof(features));
            }

            Engine = engine;
            FileSystem = fileSystem;
            Features = features;

            for (var i = 0; i < features.Count; i++)
            {
                features[i].ProjectEngine = this;
            }
        }

        public override RazorProjectFileSystem FileSystem { get; }

        public override RazorEngine Engine { get; }

        public override IReadOnlyList<IRazorProjectEngineFeature> Features { get; }

        public override RazorCodeDocument Process(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(filePath));
            }

            var projectItem = FileSystem.GetItem(filePath);
            var sourceDocument = RazorSourceDocument.ReadFrom(projectItem);
            var codeDocument = Process(sourceDocument);

            return codeDocument;
        }

        public override RazorCodeDocument Process(RazorSourceDocument sourceDocument)
        {
            if (sourceDocument == null)
            {
                throw new ArgumentNullException(nameof(sourceDocument));
            }

            var importFeature = GetRequiredFeature<IRazorImportFeature>();
            var imports = importFeature.GetImports(sourceDocument.FilePath);

            var codeDocument = RazorCodeDocument.Create(sourceDocument, imports);

            Engine.Process(codeDocument);

            return codeDocument;
        }

        private TFeature GetRequiredFeature<TFeature>() where TFeature : IRazorProjectEngineFeature
        {
            var feature = Features.OfType<TFeature>().FirstOrDefault();
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
