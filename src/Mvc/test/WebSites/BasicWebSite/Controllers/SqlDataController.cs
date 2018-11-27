// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite
{
    [NonController]
    public class SqlDataController
    {
        public int TruncateAllDbRecords()
        {
            // Return no. of tables truncated
            return 7;
        }
    }
}