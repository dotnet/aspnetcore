// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.AspNetCore.Diagnostics
{
    public class StatusCodeReExecuteFeature : IStatusCodeReExecuteFeature
    {
        public string OriginalPath { get; set; } = default!;

        public string OriginalPathBase { get; set; } = default!;

        public string? OriginalQueryString { get; set; }
    }
}
