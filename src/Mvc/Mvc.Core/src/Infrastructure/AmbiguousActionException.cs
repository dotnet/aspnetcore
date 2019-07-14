// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    /// <summary>
    /// An exception which indicates multiple matches in action selection.
    /// </summary>
    [Serializable]
    public class AmbiguousActionException : InvalidOperationException
    {
        /// <summary>
        /// Creates a new instance of <see cref="AmbiguousActionException" />.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public AmbiguousActionException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Framework infrastructure. Do not call directly.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected AmbiguousActionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
