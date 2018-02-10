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

        public override RazorCodeDocument Process(RazorProjectItem projectItem)
        {
            if (projectItem == null)
            {
                throw new ArgumentNullException(nameof(projectItem));
            }

            var importFeature = GetRequiredFeature<IRazorImportFeature>();
            var imports = importFeature.GetImports(projectItem);
            var sourceDocument = RazorSourceDocument.ReadFrom(projectItem);

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
