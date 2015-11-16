// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace RequestServicesWebSite
{
    [Route("RequestScoped/[action]")]
    public class RequestScopedServiceController
    {
        [HttpGet]
        public string FromController([FromServices] RequestIdService requestIdService)
        {
            return requestIdService.RequestId;
        }
    }
}