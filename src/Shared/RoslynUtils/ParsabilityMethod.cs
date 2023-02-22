// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Analyzers.Infrastructure;

internal enum ParsabilityMethod
{
    NotParsable,
    String,
    IParsable,
    Enum,
    TryParse,
    TryParseWithFormatProvider,
    Uri
}
