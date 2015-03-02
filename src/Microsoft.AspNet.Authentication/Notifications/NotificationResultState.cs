// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;

namespace Microsoft.AspNet.Authentication.Notifications
{
    public enum NotificationResultState
    {
        /// <summary>
        /// Continue with normal processing.
        /// </summary>
        Continue,

        /// <summary>
        /// Discontinue processing the request in the current middleware and pass control to the next one.
        /// </summary>
        Skipped,

        /// <summary>
        /// Discontinue all processing for this request.
        /// </summary>
        HandledResponse
    }
}