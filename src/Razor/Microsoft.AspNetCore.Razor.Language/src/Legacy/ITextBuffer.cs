// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal interface ITextBuffer
{
    int Length { get; }
    int Position { get; set; }
    int Read();
    int Peek();
}
