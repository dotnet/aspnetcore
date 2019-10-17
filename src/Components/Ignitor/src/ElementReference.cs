// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
