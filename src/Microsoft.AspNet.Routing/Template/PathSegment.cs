// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Routing.Template
{
    // Represents a segment of a URI such as a separator or content
    public abstract class PathSegment
    {
#if ROUTE_DEBUGGING
        public abstract string LiteralText
        {
            get;
        }
#endif
    }
}
