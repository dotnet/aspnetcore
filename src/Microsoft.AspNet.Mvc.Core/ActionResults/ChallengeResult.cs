// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    public class ChallengeResult : ActionResult
    {
        public ChallengeResult()
            : this(new string[] { })
        {
        }

        public ChallengeResult(string authenticationScheme)
            : this(new[] { authenticationScheme })
        {
        }

        public ChallengeResult(IList<string> authenticationSchemes)
            : this(authenticationSchemes, properties: null)
        {
        }

        public ChallengeResult(AuthenticationProperties properties)
            : this(new string[] { }, properties)
        {
        }

        public ChallengeResult(string authenticationScheme, AuthenticationProperties properties)
            : this(new[] { authenticationScheme }, properties)
        {
        }

        public ChallengeResult(IList<string> authenticationSchemes, AuthenticationProperties properties)
        {
            AuthenticationSchemes = authenticationSchemes;
            Properties = properties;
        }

        public IList<string> AuthenticationSchemes { get; set; }

        public AuthenticationProperties Properties { get; set; }

        public override void ExecuteResult([NotNull] ActionContext context)
        {
            var response = context.HttpContext.Response;
            response.Challenge(Properties, AuthenticationSchemes);
        }
    }
}
