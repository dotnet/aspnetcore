// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace BasicWebSite
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

        [Authorize("Impossible")]
        public string Impossible()
        {
            throw new Exception("Shouldn't be invoked.");
        }
    }
}
