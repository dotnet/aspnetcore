// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Routing.Template
{
    // Represents a parameter subsegment of a ContentPathSegment
    internal sealed class PathParameterSubsegment : PathSubsegment
    {
        public PathParameterSubsegment(string parameterName)
        {
            if (parameterName.StartsWith("*", StringComparison.Ordinal))
            {
                ParameterName = parameterName.Substring(1);
                IsCatchAll = true;
            }
            else
            {
                ParameterName = parameterName;
            }
        }

        public bool IsCatchAll { get; private set; }

        public string ParameterName { get; private set; }

#if ROUTE_DEBUGGING
        public override string LiteralText
        {
            get
            {
                return "{" + (IsCatchAll ? "*" : String.Empty) + ParameterName + "}";
            }
        }

        public override string ToString()
        {
            return "{" + (IsCatchAll ? "*" : String.Empty) + ParameterName + "}";
        }
#endif
    }
}
