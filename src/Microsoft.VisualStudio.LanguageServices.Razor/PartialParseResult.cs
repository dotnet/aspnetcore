// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    [Flags]
    public enum PartialParseResult
    {
        Rejected = 1,

        Accepted = 2,

        Provisional = 4,

        SpanContextChanged = 8,

        AutoCompleteBlock = 16
    }
}
