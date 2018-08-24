// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    [Flags]
    internal enum AcceptedCharactersInternal
    {
        None = 0,
        NewLine = 1,
        Whitespace = 2,

        NonWhitespace = 4,

        AllWhitespace = NewLine | Whitespace,
        Any = AllWhitespace | NonWhitespace,

        AnyExceptNewline = NonWhitespace | Whitespace
    }
}
