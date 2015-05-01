// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Web.Http;

namespace WebApiCompatShimWebSite
{
    // Each of these is mapped to an unnamed action with the corresponding http verb, and also
    // a named action with the corresponding http verb.
    [ActionSelectionFilter]
    public class WebAPIActionConventionsController : ApiController
    {
        public void GetItems()
        {
        }

        public void PostItems()
        {
        }

        public void PutItems()
        {
        }

        public void DeleteItems()
        {
        }

        public void PatchItems()
        {
        }

        public void HeadItems()
        {
        }

        public void OptionsItems()
        {
        }
    }
}