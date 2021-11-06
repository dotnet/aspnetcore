// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract class RazorEnginePhaseBase : IRazorEnginePhase
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
            OnIntialized();
        }
    }

    public void Execute(RazorCodeDocument codeDocument)
    {
        if (codeDocument == null)
        {
            throw new ArgumentNullException(nameof(codeDocument));
        }

        if (Engine == null)
        {
            throw new InvalidOperationException(Resources.FormatPhaseMustBeInitialized(nameof(Engine)));
        }

        ExecuteCore(codeDocument);
    }

    protected T GetRequiredFeature<T>()
    {
        if (Engine == null)
        {
            throw new InvalidOperationException(Resources.FormatFeatureMustBeInitialized(nameof(Engine)));
        }

        var feature = Engine.Features.OfType<T>().FirstOrDefault();
        ThrowForMissingFeatureDependency<T>(feature);

        return feature;
    }

    protected void ThrowForMissingDocumentDependency<TDocumentDependency>(TDocumentDependency value)
    {
        if (value == null)
        {
            throw new InvalidOperationException(
                Resources.FormatPhaseDependencyMissing(
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
                Resources.FormatPhaseDependencyMissing(
                    GetType().Name,
                    typeof(TEngineDependency).Name,
                    typeof(RazorEngine).Name));
        }
    }

    protected virtual void OnIntialized()
    {
    }

    protected abstract void ExecuteCore(RazorCodeDocument codeDocument);
}
