// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;

namespace ControllersFromServicesClassLibrary
{
    public class QueryValueService
    {
        private readonly HttpContext _context;

        public QueryValueService(IHttpContextAccessor httpContextAccessor)
        {
            _context = httpContextAccessor.Value;
        }

        public string GetValue()
        {
            return _context.Request.Query["value"];
        }
    }
}