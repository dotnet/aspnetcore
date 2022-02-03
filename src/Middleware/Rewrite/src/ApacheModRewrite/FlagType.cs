// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Rewrite.ApacheModRewrite;

internal enum FlagType
{
    EscapeBackreference,
    Chain,
    Cookie,
    DiscardPath,
    Env,
    End,
    Forbidden,
    Gone,
    Handler,
    Last,
    Next,
    NoCase,
    NoEscape,
    NoSubReq,
    NoVary,
    Or,
    Proxy,
    PassThrough,
    QSAppend,
    QSDiscard,
    QSLast,
    Redirect,
    Skip,
    Type
}
