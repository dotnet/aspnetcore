// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract class RazorEngineFeatureBase : IRazorEngineFeature
{
    private RazorEngine _engine;

    public RazorEngine Engine
    {
        get { return _engine; }
        set
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            _engine = value;
            OnInitialized();
        }
    }

    protected TFeature GetRequiredFeature<TFeature>() where TFeature : IRazorEngineFeature
    {
        if (Engine == null)
        {
            throw new InvalidOperationException(Resources.FormatFeatureMustBeInitialized(nameof(Engine)));
        }

        var feature = Engine.GetFeature<TFeature>();
        ThrowForMissingFeatureDependency<TFeature>(feature);

        return feature;
    }

    protected void ThrowForMissingDocumentDependency<TDocumentDependency>(TDocumentDependency value)
    {
        if (value == null)
        {
            throw new InvalidOperationException(
                Resources.FormatFeatureDependencyMissing(
                    GetType().Name,
                    typeof(TDocumentDependency).Name,
                    typeof(RazorCodeDocument).Name));
        }
    }

    protected void ThrowForMissingFeatureDependency<TEngineDependency>(TEngineDependency value)
    {
        if (value == null)
        {
            throw new InvalidOperationException(
                Resources.FormatFeatureDependencyMissing(
                    GetType().Name,
                    typeof(TEngineDependency).Name,
                    typeof(RazorEngine).Name));
        }
    }

    protected virtual void OnInitialized()
    {
    }
}
