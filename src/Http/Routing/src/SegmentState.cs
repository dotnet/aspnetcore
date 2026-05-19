// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing;

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
