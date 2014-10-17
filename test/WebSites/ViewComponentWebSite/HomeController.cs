// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ViewComponentWebSite
{
    public class HomeController
    {
        public ViewResult ViewWithAsyncComponents()
        {
            return new ViewResult();
        }

        public ViewResult ViewWithSyncComponents()
        {
            return new ViewResult();
        }

        public ViewResult ViewWithIntegerViewComponent()
        {
            return new ViewResult();
        }
    }
}