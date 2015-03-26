// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.Runtime;

namespace MvcSample.Web
{
    public class RazorPreCompilation : RazorPreCompileModule
    {
        public RazorPreCompilation(IServiceProvider provider,
                                   IApplicationEnvironment applicationEnvironment)
            : base(provider)
        {
            GenerateSymbols = string.Equals(applicationEnvironment.Configuration,
                                            "debug",
                                            StringComparison.OrdinalIgnoreCase);
        }
    }
}