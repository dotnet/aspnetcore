// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal class ComponentCodeTarget : CodeTarget
{
    private readonly RazorCodeGenerationOptions _options;

    public ComponentCodeTarget(RazorCodeGenerationOptions options, IEnumerable<ICodeTargetExtension> extensions)
    {
        _options = options;

        // Components provide some built-in target extensions that don't apply to
        // legacy documents.
        Extensions = new[] { new ComponentTemplateTargetExtension(), }.Concat(extensions).ToArray();
    }

    public ICodeTargetExtension[] Extensions { get; }

    public override IntermediateNodeWriter CreateNodeWriter()
    {
        return _options.DesignTime ? (IntermediateNodeWriter)new ComponentDesignTimeNodeWriter() : new ComponentRuntimeNodeWriter();
    }

    public override TExtension GetExtension<TExtension>()
    {
        for (var i = 0; i < Extensions.Length; i++)
        {
            var match = Extensions[i] as TExtension;
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    public override bool HasExtension<TExtension>()
    {
        for (var i = 0; i < Extensions.Length; i++)
        {
            var match = Extensions[i] as TExtension;
            if (match != null)
            {
                return true;
            }
        }

        return false;
    }
}
