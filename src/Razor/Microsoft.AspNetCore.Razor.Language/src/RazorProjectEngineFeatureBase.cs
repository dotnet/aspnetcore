// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract class RazorProjectEngineFeatureBase : IRazorProjectEngineFeature
{
    private RazorProjectEngine _projectEngine;

    public virtual RazorProjectEngine ProjectEngine
    {
        get => _projectEngine;
        set
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            _projectEngine = value;
            OnInitialized();
        }
    }

    protected virtual void OnInitialized()
    {
    }
}
