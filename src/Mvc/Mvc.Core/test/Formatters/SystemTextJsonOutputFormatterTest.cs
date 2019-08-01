// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    public class SystemTextJsonOutputFormatterTest : JsonOutputFormatterTestBase
    {
        protected override TextOutputFormatter GetOutputFormatter()
        {
            return new SystemTextJsonOutputFormatter(new JsonOptions());
        }
    }
}
