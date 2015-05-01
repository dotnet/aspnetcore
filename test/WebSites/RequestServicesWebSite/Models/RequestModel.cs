// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace RequestServicesWebSite
{
    public class RequestModel
    {
        [FromServices]
        public RequestIdService RequestIdService { get; set; }
    }
}