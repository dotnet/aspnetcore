// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace FormatterWebSite
{
    public class ErrorInfo
    {
        public string Source { get; set; }

        public string ActionName { get; set; }

        public string ParameterName { get; set; }

        public List<string> Errors { get; set; }
    }
}