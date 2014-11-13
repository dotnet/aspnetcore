// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Represents a message
    /// </summary>
    public class IdentityMessage
    {
        /// <summary>
        ///     Target for the message, i.e. email or phone number
        /// </summary>
        public virtual string Destination { get; set; }

        /// <summary>
        ///     Subject
        /// </summary>
        public virtual string Subject { get; set; }

        /// <summary>
        ///     Message contents
        /// </summary>
        public virtual string Body { get; set; }
    }
}