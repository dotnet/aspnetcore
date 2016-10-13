// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal interface ITextBuffer
    {
        int Length { get; }
        int Position { get; set; }
        int Read();
        int Peek();
    }
}
