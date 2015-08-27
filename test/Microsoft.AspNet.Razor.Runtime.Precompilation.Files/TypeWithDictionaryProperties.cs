// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNet.Razor.Runtime.Precompilation
{
    public class TypeWithDictionaryProperties
    {
        public IDictionary<string, string> RouteValues1 { get; set; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<int, string> RouteValues2 { get; set; } =
            new Dictionary<int, string>();

        public Dictionary<List<string>, float> RouteValues3 { get; set; } =
            new Dictionary<List<string>, float>();

        public IDictionary<string, ParserResults> CustomDictionary { get; set; } =
            new Dictionary<string, ParserResults>();

        public IDictionary NonGenericDictionary { get; set; } =
            new Dictionary<string, string>();

        public object ObjectType { get; set; } =
            new Dictionary<string, string>();
    }
}
