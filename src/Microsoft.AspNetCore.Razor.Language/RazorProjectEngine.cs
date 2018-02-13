// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class RazorProjectEngine
    {
        public abstract RazorProjectFileSystem FileSystem { get; }

        public abstract RazorEngine Engine { get; }

        public abstract IReadOnlyList<IRazorProjectEngineFeature> Features { get; }

        public abstract RazorCodeDocument Process(RazorProjectItem projectItem);

        public static RazorProjectEngine Create(RazorProjectFileSystem fileSystem) => Create(fileSystem, configure: null);

        public static RazorProjectEngine Create(RazorProjectFileSystem fileSystem, Action<RazorProjectEngineBuilder> configure) => Create(fileSystem, RazorConfiguration.Default, configure);

        public static RazorProjectEngine Create(
            RazorProjectFileSystem fileSystem,
            RazorConfiguration configuration,
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

            AddDefaults(builder);

            if (configuration.DesignTime)
            {
                AddDesignTimeDefaults(builder);
            }
            else
            {
                AddRuntimeDefaults(builder);
            }

            configure?.Invoke(builder);

            return builder.Build();
        }

        private static void AddDefaults(RazorProjectEngineBuilder builder)
        {
            builder.Features.Add(new DefaultRazorImportFeature());
        }

        private static void AddDesignTimeDefaults(RazorProjectEngineBuilder builder)
        {
            var engineFeatures = new List<IRazorEngineFeature>();
            RazorEngine.AddDefaultFeatures(engineFeatures);
            RazorEngine.AddDefaultDesignTimeFeatures(builder.Configuration, engineFeatures);

            AddEngineFeaturesAndPhases(builder, engineFeatures);
        }

        private static void AddRuntimeDefaults(RazorProjectEngineBuilder builder)
        {
            var engineFeatures = new List<IRazorEngineFeature>();
            RazorEngine.AddDefaultFeatures(engineFeatures);
            RazorEngine.AddDefaultRuntimeFeatures(builder.Configuration, engineFeatures);

            AddEngineFeaturesAndPhases(builder, engineFeatures);
        }

        private static void AddEngineFeaturesAndPhases(RazorProjectEngineBuilder builder, IReadOnlyList<IRazorEngineFeature> engineFeatures)
        {
            for (var i = 0; i < engineFeatures.Count; i++)
            {
                var engineFeature = engineFeatures[i];
                builder.Features.Add(engineFeature);
            }

            RazorEngine.AddDefaultPhases(builder.Phases);
        }
    }
}
