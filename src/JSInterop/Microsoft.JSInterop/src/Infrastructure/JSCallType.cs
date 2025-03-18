// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.JSInterop.Infrastructure;

public enum JSCallType : int
{
    FunctionCall = 1,
    NewCall = 2,
    GetValue = 3,
    SetValue = 4,
}
