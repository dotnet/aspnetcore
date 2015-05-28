// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Chunks.Generators;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class UnclassifiedCodeSpanConstructor
    {
        private readonly SpanConstructor _self;

        public UnclassifiedCodeSpanConstructor(SpanConstructor self)
        {
            _self = self;
        }

        public SpanConstructor As(ISpanChunkGenerator codeGenerator)
        {
            return _self.With(codeGenerator);
        }
    }
}