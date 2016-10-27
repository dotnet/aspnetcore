// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    public abstract class CsHtmlMarkupParserTestBase : MarkupParserTestBase
    {
        internal override ISet<string> KeywordSet
        {
            get { return CSharpCodeParser.DefaultKeywords; }
        }

        internal override BlockFactory CreateBlockFactory()
        {
            return new BlockFactory(Factory ?? CreateSpanFactory());
        }
    }
}
