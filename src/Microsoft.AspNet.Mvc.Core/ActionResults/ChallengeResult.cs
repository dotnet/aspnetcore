// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Http.Security;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    public class ChallengeResult : ActionResult
    {
        public ChallengeResult()
            : this(new string[] { })
        {
        }

        public ChallengeResult(string authenticationType)
            : this(new[] { authenticationType })
        {
        }

        public ChallengeResult(IList<string> authenticationTypes)
            : this(authenticationTypes, properties: null)
        {
        }

        public ChallengeResult(AuthenticationProperties properties)
            : this(new string[] { }, properties)
        {
        }

        public ChallengeResult(string authenticationType, AuthenticationProperties properties)
            : this(new[] { authenticationType }, properties)
        {
        }

        public ChallengeResult(IList<string> authenticationTypes, AuthenticationProperties properties)
        {
            AuthenticationTypes = authenticationTypes;
            Properties = properties;
        }

        public IList<string> AuthenticationTypes { get; set; }

        public AuthenticationProperties Properties { get; set; }

        public override void ExecuteResult([NotNull] ActionContext context)
        {
            var response = context.HttpContext.Response;
            response.Challenge(Properties, AuthenticationTypes);
        }
    }
}
