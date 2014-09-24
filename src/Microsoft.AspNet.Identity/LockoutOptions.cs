// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Identity
{
    public class LockoutOptions
    {
        /// <summary>
        ///     If true, will enable user lockout when users are created
        /// </summary>
        public bool EnabledByDefault { get; set; } = false;

        /// <summary>
        ///     Number of access attempts allowed for a user before lockout (if enabled)
        /// </summary>
        public int MaxFailedAccessAttempts { get; set; } = 5;

        /// <summary>
        ///     Default amount of time an user is locked out for after MaxFailedAccessAttempsBeforeLockout is reached
        /// </summary>
        public TimeSpan DefaultLockoutTimeSpan { get; set; } = TimeSpan.FromMinutes(5);
    }
}