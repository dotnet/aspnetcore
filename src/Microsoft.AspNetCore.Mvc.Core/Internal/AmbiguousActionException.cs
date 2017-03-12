// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
#if NET46
using System.Runtime.Serialization;
#endif

namespace Microsoft.AspNetCore.Mvc.Internal
{
    /// <summary>
    /// An exception which indicates multiple matches in action selection.
    /// </summary>
#if NET46
    [Serializable]
#elif NETSTANDARD1_6
#else
#error target frameworks need to be updated
#endif
    public class AmbiguousActionException : InvalidOperationException
    {
        public AmbiguousActionException(string message)
            : base(message)
        {
        }

#if NET46
        protected AmbiguousActionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#elif NETSTANDARD1_6
#else
#error target frameworks need to be updated
#endif
    }
}
