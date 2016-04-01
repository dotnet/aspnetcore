// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNet.Razor.Parser.SyntaxTree
{
    [Flags]
    public enum AcceptedCharacters
    {
        None = 0,
        NewLine = 1,
        WhiteSpace = 2,

        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "NonWhite", Justification = "This is not a compound word, it is two words")]
        NonWhiteSpace = 4,

        AllWhiteSpace = NewLine | WhiteSpace,
        Any = AllWhiteSpace | NonWhiteSpace,

        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Newline", Justification = "This would be a breaking change to a previous released API")]
        AnyExceptNewline = NonWhiteSpace | WhiteSpace
    }
}
