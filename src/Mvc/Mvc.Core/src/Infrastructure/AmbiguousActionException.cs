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
    internal class AmbiguousActionException : InvalidOperationException
    {
        public AmbiguousActionException(string message)
            : base(message)
        {
        }

        protected AmbiguousActionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
