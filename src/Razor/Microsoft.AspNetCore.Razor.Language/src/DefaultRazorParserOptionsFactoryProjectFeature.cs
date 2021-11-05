// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language;

internal class DefaultRazorParserOptionsFactoryProjectFeature : RazorProjectEngineFeatureBase, IRazorParserOptionsFactoryProjectFeature
{
    private IConfigureRazorParserOptionsFeature[] _configureOptions;

    protected override void OnInitialized()
    {
        _configureOptions = ProjectEngine.EngineFeatures.OfType<IConfigureRazorParserOptionsFeature>().ToArray();
    }

    public RazorParserOptions Create(string fileKind, Action<RazorParserOptionsBuilder> configure)
    {
        var builder = new DefaultRazorParserOptionsBuilder(ProjectEngine.Configuration, fileKind);
        configure?.Invoke(builder);

        for (var i = 0; i < _configureOptions.Length; i++)
        {
            _configureOptions[i].Configure(builder);
        }

        var options = builder.Build();
        return options;
    }
}
