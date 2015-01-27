// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace RequestServicesWebSite
{
    [Route("RequestScoped/[action]")]
    public class RequestScopedServiceController
    {
        [FromServices]
        public RequestIdService RequestIdService { get; set; }

        [HttpGet]
        public string FromController()
        {
            return RequestIdService.RequestId;
        }
    }
}