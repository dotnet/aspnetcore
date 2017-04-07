// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
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

        public override DocumentWriter CreateWriter(CSharpRenderingContext context)
        {
            PageStructureCSharpRenderer renderer;
            if (_options.DesignTimeMode)
            {
                renderer =  new DesignTimeCSharpRenderer(this, context);
            }
            else
            {
                renderer = new RuntimeCSharpRenderer(this, context);
            }

            return new DefaultDocumentWriter(this, context, renderer);
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
