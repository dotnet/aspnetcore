// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Http.Metadata
{
    /// <summary>
    /// Metadata that specifies the supported request content types.
    /// </summary>
    public sealed class AcceptsMetadata : IAcceptsMetadata
    {
        /// <summary>
        /// Creates a new instance of <see cref="AcceptsMetadata"/>.
        /// </summary>
        public AcceptsMetadata(string[] contentTypes)
        {
            if (contentTypes == null)
            {
                throw new ArgumentNullException(nameof(contentTypes));
            }

            ContentTypes = contentTypes;
        }

        /// <summary>
        /// Creates a new instance of <see cref="AcceptsMetadata"/> with a type.
        /// </summary>
        public AcceptsMetadata(Type? type, string[] contentTypes)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));

            if (contentTypes == null)
            {
                throw new ArgumentNullException(nameof(contentTypes));
            }

            ContentTypes = contentTypes;
        }

        /// <summary>
        /// Gets the supported request content types. 
        /// </summary>
        public IReadOnlyList<string> ContentTypes { get; }


        /// <summary>
        /// Accepts request content types of any shape. 
        /// </summary>
        public Type? Type { get; }
    }
}
