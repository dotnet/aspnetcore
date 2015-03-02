// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Authentication.Notifications
{
    public class BaseNotification<TOptions> : BaseContext<TOptions>
    {
        protected BaseNotification(HttpContext context, TOptions options) : base(context, options)
        {
        }

        public NotificationResultState State { get; set; }

        public bool HandledResponse
        {
            get { return State == NotificationResultState.HandledResponse; }
        }

        public bool Skipped
        {
            get { return State == NotificationResultState.Skipped; }
        }

        /// <summary>
        /// Discontinue all processing for this request and return to the client.
        /// The caller is responsible for generating the full response.
        /// Set the <see cref="AuthenticationTicket"/> to trigger SignIn.
        /// </summary>
        public void HandleResponse()
        {
            State = NotificationResultState.HandledResponse;
        }

        /// <summary>
        /// Discontinue processing the request in the current middleware and pass control to the next one.
        /// SignIn will not be called.
        /// </summary>
        public void SkipToNextMiddleware()
        {
            State = NotificationResultState.Skipped;
        }

        /// <summary>
        /// Gets or set the <see cref="AuthenticationTicket"/> to return if this notification signals it handled the notification.
        /// </summary>
        public AuthenticationTicket AuthenticationTicket { get; set; }
    }
}