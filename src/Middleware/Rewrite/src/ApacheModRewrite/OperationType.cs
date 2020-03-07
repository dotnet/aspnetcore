// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite.ApacheModRewrite
{
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
}
