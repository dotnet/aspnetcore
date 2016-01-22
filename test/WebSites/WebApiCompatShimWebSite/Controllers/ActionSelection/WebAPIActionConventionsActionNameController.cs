// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Web.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApiCompatShimWebSite
{
    // The verb is still inferred by the METHOD NAME not the action name.
    [ActionSelectionFilter]
    public class WebAPIActionConventionsActionNameController : ApiController
    {
        [ActionName("GetItems")]
        public void PostItems()
        {
        }
    }
}