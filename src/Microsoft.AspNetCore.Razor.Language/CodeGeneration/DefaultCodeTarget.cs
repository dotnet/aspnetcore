// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    internal class DefaultCodeTarget : CodeTarget
    {
        private readonly RazorCodeGenerationOptions _options;

        public DefaultCodeTarget(RazorCodeGenerationOptions options, IEnumerable<ICodeTargetExtension> extensions)
        {
            _options = options;
            Extensions = extensions.ToArray();
        }

        public ICodeTargetExtension[] Extensions { get; }

        public override DocumentWriter CreateWriter(CodeRenderingContext context)
        {
            return new DefaultDocumentWriter(this, context);
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
