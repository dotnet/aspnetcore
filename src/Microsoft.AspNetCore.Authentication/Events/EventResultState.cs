// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Authentication
{
    public enum EventResultState
    {
        /// <summary>
        /// Continue with normal processing.
        /// </summary>
        Continue,

        /// <summary>
        /// Discontinue processing the request.
        /// </summary>
        Skipped,

        /// <summary>
        /// Discontinue all processing for this request.
        /// </summary>
        HandledResponse
    }
}