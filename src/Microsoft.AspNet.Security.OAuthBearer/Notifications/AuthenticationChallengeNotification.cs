// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Security.Notifications;

namespace Microsoft.AspNet.Security.OAuthBearer
{
    public class AuthenticationChallengeNotification<TOptions> : BaseNotification<TOptions>
    {
        public AuthenticationChallengeNotification(HttpContext context, TOptions options) : base(context, options)
        {
        }
    }
}
