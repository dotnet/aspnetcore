// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using ModelBindingWebSite.Models;

namespace ModelBindingWebSite.Controllers
{
    public class MultiplePropertiesFromBodyController : Controller
    {
        [FromBody]
        public User SiteUser { get; set; }

        [FromBody]
        public Country Country { get; set; }

        public User GetUser()
        {
            return SiteUser;
        }
    }
}