// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Components.Razor
{
    /// <summary>
    /// Directs a <see cref="DocumentWriter"/> to use <see cref="BlazorRuntimeNodeWriter"/>.
    /// </summary>
    internal class BlazorCodeTarget : CodeTarget
    {
        private readonly RazorCodeGenerationOptions _options;

        public BlazorCodeTarget(RazorCodeGenerationOptions options, IEnumerable<ICodeTargetExtension> extensions)
        {
            _options = options;
            Extensions = extensions.ToArray();
        }

        public ICodeTargetExtension[] Extensions { get; }

        public override IntermediateNodeWriter CreateNodeWriter()
        {
            return _options.DesignTime ? (BlazorNodeWriter)new BlazorDesignTimeNodeWriter() : new BlazorRuntimeNodeWriter();
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
}
