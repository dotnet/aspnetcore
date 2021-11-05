// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract class IntermediateNodePassBase : RazorEngineFeatureBase
{
    /// <summary>
    /// The default implementation of the <see cref="IRazorEngineFeature"/>s that run in a
    /// <see cref="IRazorEnginePhase"/> will use this value for its Order property.
    /// </summary>
    /// <remarks>
    /// This value is chosen in such a way that the default implementation runs after the other
    /// custom <see cref="IRazorEngineFeature"/> implementations for a particular <see cref="IRazorEnginePhase"/>.
    /// </remarks>
    public static readonly int DefaultFeatureOrder = 1000;

    public virtual int Order { get; }

    public void Execute(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
    {
        if (codeDocument == null)
        {
            throw new ArgumentNullException(nameof(codeDocument));
        }

        if (documentNode == null)
        {
            throw new ArgumentNullException(nameof(documentNode));
        }

        if (Engine == null)
        {
            throw new InvalidOperationException(Resources.FormatPhaseMustBeInitialized(nameof(Engine)));
        }

        ExecuteCore(codeDocument, documentNode);
    }

    protected abstract void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode);
}
