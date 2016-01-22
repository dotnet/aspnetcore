// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace FiltersWebSite
{
    [Authorize("Api")]
    public class AuthorizeUserController : Controller
    {
        [Authorize("Api-Manager")]
        public string ApiManagers()
        {
            return "Hello World!";
        }

        [Authorize(Roles = "Administrator")]
        public string AdminRole()
        {
            return "Hello World!";
        }

        [Authorize("Interactive")]
        public string InteractiveUsers()
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