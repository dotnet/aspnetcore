// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language;

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

    public override RazorConfiguration Configuration { get; }

    public override RazorProjectFileSystem FileSystem { get; }

    public override ICollection<IRazorFeature> Features { get; }

    public override IList<IRazorEnginePhase> Phases { get; }

    public override RazorProjectEngine Build()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        var engine = RazorEngine.CreateEmpty(ConfigureRazorEngine);
#pragma warning restore CS0618 // Type or member is obsolete
        var projectFeatures = Features.OfType<IRazorProjectEngineFeature>().ToArray();
        var projectEngine = new DefaultRazorProjectEngine(Configuration, engine, FileSystem, projectFeatures);

        return projectEngine;
    }

#pragma warning disable CS0618 // Type or member is obsolete
    private void ConfigureRazorEngine(IRazorEngineBuilder engineBuilder)
#pragma warning disable CS0618 // Type or member is obsolete
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
