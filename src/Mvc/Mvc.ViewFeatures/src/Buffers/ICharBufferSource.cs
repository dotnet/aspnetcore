// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;

internal interface ICharBufferSource
{
    char[] Rent(int bufferSize);

    void Return(char[] buffer);
}
