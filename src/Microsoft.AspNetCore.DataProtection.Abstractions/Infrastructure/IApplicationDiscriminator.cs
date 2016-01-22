// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;

namespace Microsoft.AspNetCore.DataProtection.Infrastructure
{
    /// <summary>
    /// Provides information used to discriminate applications.
    /// </summary>
    /// <remarks>
    /// This type supports the data protection system and is not intended to be used
    /// by consumers.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IApplicationDiscriminator
    {
        /// <summary>
        /// An identifier that uniquely discriminates this application from all other
        /// applications on the machine.
        /// </summary>
        string Discriminator { get; }
    }
}
