// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.AspNetCore.Routing.Matching
{
    /// <summary>
    /// An exception which indicates multiple matches in endpoint selection.
    /// </summary>
    [Serializable]
    internal class AmbiguousMatchException : Exception
    {
        public AmbiguousMatchException(string message)
            : base(message)
        {
        }

        protected AmbiguousMatchException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}