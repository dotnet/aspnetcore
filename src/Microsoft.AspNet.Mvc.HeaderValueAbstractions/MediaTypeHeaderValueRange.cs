// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc
{
    public enum MediaTypeHeaderValueRange
    {
        /// <summary>
        /// Not a media type range
        /// </summary>
        None = 0,

        /// <summary>
        /// A subtype media range, e.g. "application/*".
        /// </summary>
        SubtypeMediaRange,

        /// <summary>
        /// An all media range, e.g. "*/*".
        /// </summary>
        AllMediaRange,
    }
}
