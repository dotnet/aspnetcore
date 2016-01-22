// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace InlineConstraintSample.Web.Controllers
{
    public class StoreController : Controller
    {
        [Route("store/[action]/{id:guid?}")]
        public IDictionary<string, object> GetStoreById(Guid id)
        {
            return RouteData.Values;
        }

        [Route("store/[action]/{location:alpha:minlength(3):maxlength(10)}")]
        public IDictionary<string, object> GetStoreByLocation(string location)
        {
            return RouteData.Values;
        }
    }
}