// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Authentication.Certificate
{
    [Flags]
    public enum CertificateTypes 
    {
        Chained = 1,
        SelfSigned = 2,
        All = 3
    }
}
