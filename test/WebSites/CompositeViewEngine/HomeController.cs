// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace CompositeViewEngine
{
    public class HomeController
    {
        public ViewResult Index()
        {
            return new ViewResult();
        }

        public ViewResult TestView()
        {
            return new ViewResult { ViewName = "test-view" };
        }
    }
}