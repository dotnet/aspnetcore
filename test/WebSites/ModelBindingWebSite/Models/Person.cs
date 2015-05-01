// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace ModelBindingWebSite.Models
{
    public class Person
    {
        public string Name { get; set; }

        public int Age { get; set; }

        public Person Parent { get; set; }

        public List<Person> Dependents { get; set; }

        public Dictionary<string, string> Attributes { get; set; }
    }
}