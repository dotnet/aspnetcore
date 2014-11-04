// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace RequestServicesWebSite
{
    public class RequestIdViewComponent : ViewComponent
    {
        [Activate]
        public RequestIdService RequestIdService { get; set; }

        public IViewComponentResult Invoke()
        {
            return Content(RequestIdService.RequestId);
        }
    }
}