// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.JsonPatch.Operations;

public enum OperationType
{
    Add,
    Remove,
    Replace,
    Move,
    Copy,
    Test,
    Invalid
}
