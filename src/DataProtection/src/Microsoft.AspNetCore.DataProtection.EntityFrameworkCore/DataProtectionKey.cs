// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.DataProtection.EntityFrameworkCore
{
    /// <summary>
    /// Code first model used by <see cref="EntityFrameworkCoreXmlRepository{TContext}"/>.
    /// </summary>
    public class DataProtectionKey
    {
        /// <summary>
        /// The entity identifier of the <see cref="DataProtectionKey"/>.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The friendly name of the <see cref="DataProtectionKey"/>.
        /// </summary>
        public string FriendlyName { get; set; }

        /// <summary>
        /// The XML representation of the <see cref="DataProtectionKey"/>.
        /// </summary>
        public string Xml { get; set; }
    }
}
