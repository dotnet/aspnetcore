// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal abstract class ParserBase
{
    public ParserBase(ParserContext context)
    {
        Context = context;
    }

    public ParserContext Context { get; }
}
