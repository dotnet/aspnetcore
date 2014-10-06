// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc;

namespace FiltersWebSite
{
    public class ExceptionController : Controller
    {
        public string GetError(string error)
        {
            throw new InvalidOperationException(error);
        }
    }
}