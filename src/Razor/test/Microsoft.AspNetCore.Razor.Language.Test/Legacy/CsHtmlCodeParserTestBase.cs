// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public abstract class CsHtmlCodeParserTestBase : CodeParserTestBase
    {
        internal override ISet<string> KeywordSet
        {
            get { return CSharpCodeParser.DefaultKeywords; }
        }
    }
}
