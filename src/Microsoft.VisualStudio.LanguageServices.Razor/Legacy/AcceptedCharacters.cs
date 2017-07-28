// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    // ----------------------------------------------------------------------------------------------------
    // NOTE: This is only here for VisualStudio binary compatibility. This type should not be used; instead
    // use the Microsoft.CodeAnalysis.Razor variant from Microsoft.CodeAnalysis.Razor.Workspaces
    // ----------------------------------------------------------------------------------------------------
    [Flags]
    public enum AcceptedCharacters
    {
        None = 0,
        NewLine = 1,
        WhiteSpace = 2,

        NonWhiteSpace = 4,

        AllWhiteSpace = NewLine | WhiteSpace,
        Any = AllWhiteSpace | NonWhiteSpace,

        AnyExceptNewline = NonWhiteSpace | WhiteSpace
    }
}
