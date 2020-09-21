// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public sealed class MethodParameter
    {
        public IList<string> Modifiers { get; } = new List<string>();

        public string TypeName { get; set; }

        public string ParameterName { get; set; }
    }
}
