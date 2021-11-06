// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

internal class DefaultCodeTarget : CodeTarget
{
    private readonly RazorCodeGenerationOptions _options;

    public DefaultCodeTarget(RazorCodeGenerationOptions options, IEnumerable<ICodeTargetExtension> extensions)
    {
        _options = options;
        Extensions = extensions.ToArray();
    }

    public ICodeTargetExtension[] Extensions { get; }

    public override IntermediateNodeWriter CreateNodeWriter()
    {
        return _options.DesignTime ? (IntermediateNodeWriter)new DesignTimeNodeWriter() : new RuntimeNodeWriter();
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
