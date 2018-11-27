// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Web.Http;

namespace WebApiCompatShimWebSite
{
    // This action only accepts POST by default
    [ActionSelectionFilter]
    public class WebAPIActionConventionsDefaultPostController : ApiController
    {
        public void DefaultVerbIsPost()
        {
        }
    }
}