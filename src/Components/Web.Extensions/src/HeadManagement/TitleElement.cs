// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    internal struct TitleElement : IHeadElement
    {
        public string Type => "title";

        public string Title { get; set; }
    }
}
