// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Identity
{
    public class SignInOptions
    {
        /// <summary>
        ///     If set, requires a confirmed email to sign in
        /// </summary>
        public bool RequireConfirmedEmail { get; set; }

        /// <summary>
        ///     If set, requires a confirmed phone number to sign in
        /// </summary>
        public bool RequireConfirmedPhoneNumber { get; set; }
    }
}