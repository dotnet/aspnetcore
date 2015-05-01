// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Web.Http;

namespace ControllerDiscoveryConventionsWebSite
{
    public class PersonApi : ApiController
    {
        public string GetValue()
        {
            return nameof(PersonApi);
        }
    }
}