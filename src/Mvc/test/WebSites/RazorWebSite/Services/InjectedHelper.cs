// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace RazorWebSite
{
    public class InjectedHelper
    {
        public string Greet(Person person)
        {
            return "Hello " + person.Name;
        }
    }
}