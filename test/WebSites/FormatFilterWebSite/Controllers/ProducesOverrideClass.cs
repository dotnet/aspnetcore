// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace FormatFilterWebSite
{
    [Produces("application/custom_ProducesOverrideController")]
    public class ProducesOverrideController : ProducesBaseController
    {
        [FormatFilter]
        public override string ReturnClassName()
        {
            // should be written using the content defined at base class's action.
            return "ProducesOverrideController";
        }
    }        
}