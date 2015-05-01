// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace ModelBindingWebSite.Models
{
    public class Company
    {
        public Department Department { get; set; }

        [FromTest]
        public Person CEO { get; set; }

        public IList<Employee> Employees { get; set; }
    }
}