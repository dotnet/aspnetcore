// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite
{
    public class RequestIdViewComponent : ViewComponent
    {
        public RequestIdViewComponent(RequestIdService requestIdService)
        {
            RequestIdService = requestIdService;
        }

        private RequestIdService RequestIdService { get; }

        public IViewComponentResult Invoke()
        {
            return Content(RequestIdService.RequestId);
        }
    }
}