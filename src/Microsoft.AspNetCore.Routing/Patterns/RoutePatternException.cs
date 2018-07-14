// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.AspNetCore.Routing.Patterns
{
    [Serializable]
    public sealed class RoutePatternException : Exception
    {
        private RoutePatternException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Pattern = (string)info.GetValue(nameof(Pattern), typeof(string));
        }

        public RoutePatternException(string pattern, string message)
            : base(message)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            Pattern = pattern;
        }

        public string Pattern { get; }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Pattern), Pattern);
            base.GetObjectData(info, context);
        }
    }
}
