// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Evolution.CodeGeneration
{
    internal class DefaultRuntimeTarget : RuntimeTarget
    {
        private readonly RazorParserOptions _options;

        public DefaultRuntimeTarget(RazorParserOptions options)
        {
            _options = options;
        }

        internal override PageStructureCSharpRenderer CreateRenderer(CSharpRenderingContext context)
        {
            if (_options.DesignTimeMode)
            {
                return new DesignTimeCSharpRenderer(context);
            }
            else
            {
                return new RuntimeCSharpRenderer(context);
            }
        }

        public override TExtension GetExtension<TExtension>()
        {
            throw new NotImplementedException();
        }

        public override bool HasExtension<TExtension>()
        {
            throw new NotImplementedException();
        }
    }
}
