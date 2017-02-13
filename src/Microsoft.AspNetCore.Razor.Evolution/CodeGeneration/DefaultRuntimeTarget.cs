// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Evolution.CodeGeneration
{
    internal class DefaultRuntimeTarget : RuntimeTarget
    {
        private readonly RazorParserOptions _options;

        public DefaultRuntimeTarget(RazorParserOptions options, IEnumerable<IRuntimeTargetExtension> extensions)
        {
            _options = options;
            Extensions = extensions.ToArray();
        }

        public IRuntimeTargetExtension[] Extensions { get; }

        internal override PageStructureCSharpRenderer CreateRenderer(CSharpRenderingContext context)
        {
            if (_options.DesignTimeMode)
            {
                return new DesignTimeCSharpRenderer(this, context);
            }
            else
            {
                return new RuntimeCSharpRenderer(this, context);
            }
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
