// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Server.Kestrel.Http;

namespace Microsoft.AspNet.Server.KestrelTests
{
    public class TestDateHeaderValueManager : DateHeaderValueManager
    {
        public override string GetDateHeaderValue()
        {
            return DateTimeOffset.UtcNow.ToString("r");
        }
    }
}
