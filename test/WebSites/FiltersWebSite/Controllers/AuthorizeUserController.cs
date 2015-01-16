// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace FiltersWebSite
{
    [AuthorizeUser]
    [Authorize("RequireBasic")]
    public class AuthorizeUserController : Controller
    {
        [Authorize("CanViewPage")]
        public string ReturnHelloWorldOnlyForAuthorizedUser()
        {
            return "Hello World!";
        }

        [Authorize("Impossible")]
        [AllowAnonymous]
        public string AlwaysCanCallAllowAnonymous()
        {
            return "Hello World!";
        }

        [Authorize("Impossible")]
        public string Impossible()
        {
            return "Hello World!";
        }

    }
}