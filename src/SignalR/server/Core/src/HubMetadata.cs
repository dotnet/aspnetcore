// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.SignalR
{
    /// <summary>
    /// Metadata that describes the <see cref="Hub"/> information associated with a specific endpoint.
    /// </summary>
    public class HubMetadata
    {
        /// <summary>
        /// Constructs the <see cref="HubMetadata"/> of the given <see cref="Hub"/> type.
        /// </summary>
        /// <param name="hubType">The <see cref="Type"/> of the <see cref="Hub"/>.</param>
        public HubMetadata(Type hubType)
        {
            HubType = hubType;
        }

        /// <summary>
        /// The type of <see cref="Hub"/>.
        /// </summary>
        public Type HubType { get; }
    }
}
