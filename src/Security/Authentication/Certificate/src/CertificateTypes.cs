// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Authentication.Certificate
{
    /// <summary>
    /// Enum representing certificate types.
    /// </summary>
    [Flags]
    public enum CertificateTypes 
    {
        /// <summary>
        /// Chained certificates.
        /// </summary>
        Chained = 1,

        /// <summary>
        /// SelfSigned certificates.
        /// </summary>
        SelfSigned = 2,

        /// <summary>
        /// All certificates.
        /// </summary>
        All = Chained | SelfSigned
    }
}
