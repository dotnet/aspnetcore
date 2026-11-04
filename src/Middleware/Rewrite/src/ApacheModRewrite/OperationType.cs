// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Rewrite.ApacheModRewrite;

internal enum OperationType
{
    None,
    Equal,
    Greater,
    GreaterEqual,
    Less,
    LessEqual,
    NotEqual,
    Directory,
    RegularFile,
    ExistingFile,
    SymbolicLink,
    Size,
    ExistingUrl,
    Executable
}
