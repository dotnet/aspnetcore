// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace MvcMinimalSample.Web
{
    public class HomeController
    {
        public string Index()
        {
            return "Hi from MVC";
        }

        public string GetUser(int id)
        {
            return $"User: {id}";
        }
    }
}
