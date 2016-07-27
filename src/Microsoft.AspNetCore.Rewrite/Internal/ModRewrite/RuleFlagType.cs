// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite.Internal.ModRewrite
{
    public enum RuleFlagType
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
        Type,
        // Non-modrewrite rule
        FullUrl
    }
}
