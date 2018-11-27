// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal abstract class ParserBase
    {
        public ParserBase(ParserContext context)
        {
            Context = context;
        }

        public ParserContext Context { get; }
    }
}
