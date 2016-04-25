// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Diagnostics
{
    public class StatusCodeReExecuteFeature : IStatusCodeReExecuteFeature
    {
        public string OriginalPath { get; set; }

        public string OriginalPathBase { get; set; }

        public string OriginalQueryString { get; set; }
    }
}