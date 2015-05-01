// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace ModelBindingWebSite
{
    public class Result
    {
        public object Value { get; set; }

        public string[] ModelStateErrors { get; set; }
    }
}