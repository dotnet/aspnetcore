
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Rendering
{
    public class SystemTextJsonHelperTest : JsonHelperTestBase
    {
        protected override IJsonHelper GetJsonHelper()
        {
            var mvcOptions = new MvcOptions { SerializerOptions = { PropertyNamingPolicy = JsonNamingPolicy.CamelCase } };
            return new SystemTextJsonHelper(Options.Create(mvcOptions));
        }
    }
}
