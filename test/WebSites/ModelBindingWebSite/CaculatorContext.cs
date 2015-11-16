// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ModelBindingWebSite
{
    public class CalculatorContext
    {
        public int Left { get; set; }

        public int Right { get; set; }

        public char Operator { get; set; }
    }
}