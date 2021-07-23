// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Ignitor
{
    /// <summary>
    /// Represents a reference to a rendered element.
    /// </summary>
    public readonly struct ElementReference
    {
        /// <summary>
        /// Gets a unique identifier for <see cref="ElementReference" />.
        /// </summary>
        /// <remarks>
        /// The Id is unique at least within the scope of a given user/circuit.
        /// This property is public to support Json serialization and should not be used by user code.
        /// </remarks>
        public string Id { get; }

        public ElementReference(string id)
        {
            Id = id;
        }
    }
}
