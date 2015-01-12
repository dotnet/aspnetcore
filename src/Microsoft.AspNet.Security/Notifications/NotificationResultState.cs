using System;

namespace Microsoft.AspNet.Security.Notifications
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