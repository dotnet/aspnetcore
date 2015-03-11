// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.DataProtection
{
    /// <summary>
    /// Provides information used to discriminate applications.
    /// </summary>
    public interface IApplicationDiscriminator
    {
        /// <summary>
        /// An identifier that uniquely discriminates this application from all other
        /// applications on the machine.
        /// </summary>
        string Discriminator { get; }
    }
}
