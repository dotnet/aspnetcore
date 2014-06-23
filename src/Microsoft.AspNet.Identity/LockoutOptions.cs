// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Identity
{
    public class LockoutOptions
    {
        public LockoutOptions()
        {
            DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            EnabledByDefault = false;
            MaxFailedAccessAttempts = 5;
        }

        /// <summary>
        ///     If true, will enable user lockout when users are created
        /// </summary>
        public bool EnabledByDefault { get; set; }

        /// <summary>
        ///     Number of access attempts allowed for a user before lockout (if enabled)
        /// </summary>
        public int MaxFailedAccessAttempts { get; set; }

        /// <summary>
        ///     Default amount of time an user is locked out for after MaxFailedAccessAttempsBeforeLockout is reached
        /// </summary>
        public TimeSpan DefaultLockoutTimeSpan { get; set; }
    }
}