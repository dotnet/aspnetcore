// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Routing.Template
{
    // Represents a "/" separator in a URI
    internal sealed class PathSeparatorSegment : PathSegment
    {
#if ROUTE_DEBUGGING
        public override string LiteralText
        {
            get
            {
                return "/";
            }
        }

        public override string ToString()
        {
            return "\"/\"";
        }
#endif
    }
}
