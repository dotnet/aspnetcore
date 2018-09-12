// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Routing;

namespace RoutingWebSite
{
    public class TestParameterTransformer : IParameterTransformer
    {
        public string Transform(string value)
        {
            return "_" + value + "_";
        }
    }
}