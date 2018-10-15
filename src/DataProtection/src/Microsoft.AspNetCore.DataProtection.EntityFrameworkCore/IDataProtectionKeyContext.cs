// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore;

namespace Microsoft.AspNetCore.DataProtection.EntityFrameworkCore
{
    /// <summary>
    /// Interface used to store instances of <see cref="DataProtectionKey"/> in a <see cref="DbContext"/>
    /// </summary>
    public interface IDataProtectionKeyContext
    {
        /// <summary>
        /// A collection of <see cref="DataProtectionKey"/>
        /// </summary>
        DbSet<DataProtectionKey> DataProtectionKeys { get; }
    }
}
