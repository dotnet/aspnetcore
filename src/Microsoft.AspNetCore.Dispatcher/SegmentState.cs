// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Dispatcher
{
    // Segments are treated as all-or-none. We should never output a partial segment.
    // If we add any subsegment of this segment to the generated URI, we have to add
    // the complete match. For example, if the subsegment is "{p1}-{p2}.xml" and we
    // used a value for {p1}, we have to output the entire segment up to the next "/".
    // Otherwise we could end up with the partial segment "v1" instead of the entire
    // segment "v1-v2.xml".
    internal enum SegmentState
    {
        Beginning,
        Inside,
    }
}
