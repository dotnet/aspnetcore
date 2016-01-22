// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Web.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApiCompatShimWebSite
{
    // The verb is overridden by the attribute
    [ActionSelectionFilter]
    public class WebAPIActionConventionsVerbOverrideController : ApiController
    {
        [HttpGet]
        public void PostItems()
        {
        }
    }
}