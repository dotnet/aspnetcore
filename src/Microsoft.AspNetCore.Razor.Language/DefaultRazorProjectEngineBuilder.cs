// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRazorProjectEngineBuilder : RazorProjectEngineBuilder
    {
        public DefaultRazorProjectEngineBuilder(RazorConfiguration configuration, RazorProjectFileSystem fileSystem)
        {
            if (fileSystem == null)
            {
                throw new ArgumentNullException(nameof(fileSystem));
            }

            Configuration = configuration;
            FileSystem = fileSystem;
            Features = new List<IRazorFeature>();
            Phases = new List<IRazorEnginePhase>();
        }

        public override RazorProjectFileSystem FileSystem { get; }

        public override ICollection<IRazorFeature> Features { get; }

        public override IList<IRazorEnginePhase> Phases { get; }

        public override RazorConfiguration Configuration { get; }

        public override RazorProjectEngine Build()
        {
            RazorEngine engine = null;

            if (Configuration.DesignTime)
            {
                engine = RazorEngine.CreateDesignTimeEmpty(ConfigureRazorEngine);
            }
            else
            {
                engine = RazorEngine.CreateEmpty(ConfigureRazorEngine);
            }

            var projectEngineFeatures = Features.OfType<IRazorProjectEngineFeature>().ToArray();
            var projectEngine = new DefaultRazorProjectEngine(engine, FileSystem, projectEngineFeatures);

            return projectEngine;
        }

        private void ConfigureRazorEngine(IRazorEngineBuilder engineBuilder)
        {
            var engineFeatures = Features.OfType<IRazorEngineFeature>();
            foreach (var engineFeature in engineFeatures)
            {
                engineBuilder.Features.Add(engineFeature);
            }

            for (var i = 0; i < Phases.Count; i++)
            {
                engineBuilder.Phases.Add(Phases[i]);
            }
        }
    }
}
