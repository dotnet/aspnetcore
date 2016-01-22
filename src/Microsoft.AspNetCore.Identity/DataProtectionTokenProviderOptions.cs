// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Identity
{
    /// <summary>
    /// Contains options for the <see cref="DataProtectorTokenProvider{TUser}"/>.
    /// </summary>
    public class DataProtectionTokenProviderOptions
    {
        /// <summary>
        /// Gets or sets the name of the <see cref="DataProtectorTokenProvider{TUser}"/>.
        /// </summary>
        /// <value>
        /// The name of the <see cref="DataProtectorTokenProvider{TUser}"/>.
        /// </value>
        public string Name { get; set; } = "DataProtectorTokenProvider";

        /// <summary>
        /// Gets or sets the amount of time a generated token remains valid.
        /// </summary>
        /// <value>
        /// The amount of time a generated token remains valid.
        /// </value>
        public TimeSpan TokenLifespan { get; set; } = TimeSpan.FromDays(1);
    }
}